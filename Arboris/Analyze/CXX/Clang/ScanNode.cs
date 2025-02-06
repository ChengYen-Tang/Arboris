using Arboris.Aggregate;
using Arboris.Models;
using Arboris.Models.Analyze.CXX;
using ClangSharp;
using ClangSharp.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Index = ClangSharp.Index;

namespace Arboris.Analyze.CXX.Clang;

internal class ScanNode(ILogger logger, CxxAggregate cxxAggregate, Guid projectId, Index index, ProjectInfo projectConfig, string solutionPath, string[] clangArgs, Action<Result> printErrorMessage, Func<string, string> getRelativePath, Dictionary<string, byte> isFilesScaned, List<TranslationUnit> translationUnits)
{
    private readonly ILogger logger = logger;
    private readonly CxxAggregate cxxAggregate = cxxAggregate;
    private readonly Dictionary<string, byte> IsFilesScaned = isFilesScaned;
    private readonly Guid projectId = projectId;
    private readonly Index index = index;
    private readonly List<TranslationUnit> translationUnits = translationUnits;
    private readonly ProjectInfo projectConfig = projectConfig;
    private readonly string solutionPath = solutionPath;
    private readonly string[] clangArgs = clangArgs;

    private readonly Action<Result> PrintErrorMessage = printErrorMessage;
    private readonly Func<string, string> GetRelativePath = getRelativePath;
    private static readonly CXCursorKind[] validCursorKind = [
    CXCursorKind.CXCursor_ClassDecl,
        CXCursorKind.CXCursor_CXXMethod,
        CXCursorKind.CXCursor_Constructor,
        CXCursorKind.CXCursor_Destructor,
        CXCursorKind.CXCursor_FieldDecl,
        CXCursorKind.CXCursor_FunctionDecl,
        CXCursorKind.CXCursor_StructDecl,
        CXCursorKind.CXCursor_TypedefDecl];
    private static readonly CXCursorKind[] inValidCursorKind = [
        CXCursorKind.CXCursor_FriendDecl];

    public async Task Scan(IReadOnlyList<string> codeFiles, CancellationToken ct = default)
    {
        foreach (string headerFile in codeFiles)
        {
            if (ct.IsCancellationRequested)
                break;
            await Scan(headerFile, ct);
        }
    }

    /// <summary>
    /// Explore the source code file using the Clang library
    /// </summary>
    /// <param name="codeFile"> Source code file </param>
    /// <returns></returns>
    private async Task Scan(string codeFile, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        if (!IsFilesScaned.TryGetValue(codeFile, out byte value) || value == 1 || value == 2)
            return;

        IsFilesScaned[codeFile] = 1;
        logger.LogDebug("ScanNode-> CodeFile: {CodeFile}", codeFile);
        CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, Path.Combine(solutionPath, codeFile), clangArgs, []);
        using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);
        HashSet<string> includeStrings = [];
        if (tu is null)
        {
            if (translationUnit != null)
                translationUnit.Dispose();

            logger.LogWarning("TranslationUnit not found for {CodeFile}", codeFile);
            return;
        }
        translationUnits.Add(tu);
        foreach (var cursor in tu.TranslationUnitDecl.CursorChildren)
        {
            if (ct.IsCancellationRequested)
                break;
            await ExploreAstNode(cursor, includeStrings, ct: ct);
        }
        translationUnits.Remove(tu);
        IsFilesScaned[codeFile] = 2;
    }

    /// <summary>
    /// Recursively explore Abstract syntax tree (AST)
    /// </summary>
    /// <param name="cursor"> Ast node </param>
    /// <param name="nameSpace"> C++ code namespace </param>
    /// <returns></returns>
    private async Task ExploreAstNode(Cursor cursor, HashSet<string> includeStrings, string? nameSpace = null, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        if (!cursor.Location.IsFromMainFile || inValidCursorKind.Contains(cursor.CursorKind))
            return;

        cursor.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
        cursor.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
        using CXString fileName = file.Name;
        string fullFileName = fileName.ToString();
        // Absolute path
        Location fullLocation = new(fullFileName, startLine, endLine);
        // Relative path
        Location location = new(GetRelativePath(fullFileName), startLine, endLine);
        logger.LogDebug("Location-> StartLine: {StartLine}, EndLine: {EndLine}, StartColumn: {StartColumn}, EndColumn: {EndColumn}, FileName: {FileName}", startLine, endLine, startColumn, endColumn, location.FilePath);

        if (cursor.CursorKind == CXCursorKind.CXCursor_InclusionDirective)
        {
            string? includeString = File.ReadLines(fullLocation.FilePath).Skip((int)startLine - 1).FirstOrDefault();
            includeStrings.Add(includeString!);
        }
        else if (validCursorKind.Contains(cursor.CursorKind))
        {
            using CXString cXType = cursor.Handle.Type.Spelling;
            location.SourceCode = string.Join(Environment.NewLine, File.ReadLines(fullLocation.FilePath).Skip((int)startLine - 1).Take((int)endLine - (int)startLine + 1));
            location.DisplayName = GetDisplayName(cursor, fullLocation).TrimStart().TrimEnd(' ', '{', '\n', '\r', '\t');

            if (cursor is Decl decl)
            {
                decl.CanonicalDecl.Extent.Start.GetExpansionLocation(out CXFile defineFIle, out uint defineStartLine, out uint _, out uint _);
                decl.CanonicalDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint defineEndLine, out uint _, out uint _);
                using CXString defineFileName = defineFIle.Name;
                Location defineLocation = new(GetRelativePath(defineFileName.ToString()), defineStartLine, defineEndLine);
                logger.LogDebug("DefineLocation-> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", defineStartLine, defineEndLine, defineFileName);
                if (cursor.CursorKind == CXCursorKind.CXCursor_ClassDecl || cursor.CursorKind == CXCursorKind.CXCursor_StructDecl || (defineLocation == location && !cursor.CursorChildren.Any(item => item.CursorKind == CXCursorKind.CXCursor_CompoundStmt)))
                {
                    AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, location, null);
                    await InsertNode(addNode, decl);
                }
                else
                {
                    await Scan(defineLocation.FilePath, ct);
                    AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, defineLocation == location ? null : defineLocation, location);
                    Result result = await cxxAggregate.InsertorUpdateImplementationLocationAsync(addNode, includeStrings, ct);
                    PrintErrorMessage(result);
                }
            }
            else
            {
                AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, null, location);
                Result result = await cxxAggregate.InsertorUpdateImplementationLocationAsync(addNode, includeStrings, ct);
                PrintErrorMessage(result);
            }
        }

        if (cursor.CursorKind == CXCursorKind.CXCursor_Namespace)
            nameSpace = nameSpace is null ? cursor.Spelling : nameSpace + "::" + cursor.Spelling;

        foreach (var child in cursor.CursorChildren)
        {
            if (ct.IsCancellationRequested)
                break;
            await ExploreAstNode(child, includeStrings, nameSpace, ct);
        }
    }

    private async Task InsertNode(AddNode addNode, Decl? decl)
    {
        logger.LogDebug("Add Node-> ProjectId: {ProjectId}, CursorKindSpelling: {CursorKindSpelling}, Spelling: {Spelling}", projectId, addNode.CursorKindSpelling, addNode.Spelling);
        (Guid nodeId, bool isExist) = await cxxAggregate.AddDefineNodeAsync(addNode);
        if (decl is null || isExist || addNode.DefineLocation is null)
            return;

        // link class or struct member
        if (decl.LexicalDeclContext is not null && ((Decl)decl.LexicalDeclContext).CursorKind is CXCursorKind.CXCursor_ClassDecl or CXCursorKind.CXCursor_StructDecl)
        {
            ((Decl)decl.LexicalDeclContext).Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
            ((Decl)decl.LexicalDeclContext).Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
            using CXString fileName = file.Name;
            Location classLocation = new(GetRelativePath(fileName.ToString()), startLine, endLine);
            Result result = await cxxAggregate.LinkMemberAsync(projectId, projectConfig.ProjectName, classLocation, nodeId);
            Debug.Assert(result.IsSuccess, "ClassLocation not found");
        }
    }

    private static string GetDisplayName(Cursor rootCursor, Location rootLocation)
    {
        if (rootCursor.CursorChildren.Count == 0)
            return string.Join(Environment.NewLine, File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).Take((int)rootLocation.EndLine - (int)rootLocation.StartLine + 1));

        if (rootCursor.CursorKind == CXCursorKind.CXCursor_ClassDecl || rootCursor.CursorKind == CXCursorKind.CXCursor_StructDecl)
        {
            Cursor cursor = rootCursor.CursorChildren[0];
            cursor.Extent.Start.GetExpansionLocation(out CXFile _, out uint startLine, out uint _, out uint _);
            cursor.Extent.End.GetExpansionLocation(out CXFile _, out uint _, out uint _, out uint _);
            return string.Join(Environment.NewLine, File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).Take((int)startLine - 1 - (int)rootLocation.StartLine + 1));
        }
        else if (rootCursor.CursorKind == CXCursorKind.CXCursor_CXXMethod || rootCursor.CursorKind == CXCursorKind.CXCursor_FunctionDecl || rootCursor.CursorKind == CXCursorKind.CXCursor_Constructor || rootCursor.CursorKind == CXCursorKind.CXCursor_Destructor)
        {
            foreach (var child in rootCursor.CursorChildren)
            {
                if (child.CursorKind == CXCursorKind.CXCursor_CompoundStmt)
                {
                    child.Extent.Start.GetExpansionLocation(out CXFile _, out uint startLine, out uint startColumn, out uint _);
                    if ((int)startLine != (int)rootLocation.StartLine)
                        return string.Join(Environment.NewLine, File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).Take((int)startLine - (int)rootLocation.StartLine));
                    else
                    {
                        string code = File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).First();
                        return code[..((int)startColumn - 1)];
                    }
                }
            }
            return string.Join(Environment.NewLine, File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).Take((int)rootLocation.EndLine - (int)rootLocation.StartLine + 1));
        }
        else
            return string.Join(Environment.NewLine, File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).Take((int)rootLocation.EndLine - (int)rootLocation.StartLine + 1));
    }
}
