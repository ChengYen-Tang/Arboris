using Arboris.Aggregate;
using Arboris.Models;
using Arboris.Models.Analyze.CXX;
using ClangSharp;
using ClangSharp.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Index = ClangSharp.Index;

namespace Arboris.Analyze.CXX.Clang;

internal class ScanNode(ILogger logger,
    CxxAggregate cxxAggregate, Guid projectId, Index index,
    ProjectInfo projectConfig, string solutionPath, Dictionary<string, bool> isFilesScaned,
    string[] clangBaseArgs, List<TranslationUnit> translationUnits,
    Action<Result> printErrorMessage, Func<string, string> getRelativePath,
    ConcurrentDictionary<Location, ConcurrentDictionary<Guid, IReadOnlyList<string>>> memberBuffer)
    : ScanAbstract(logger, isFilesScaned, getRelativePath)
{
    private static readonly CXCursorKind[] validCursorKind = [
        CXCursorKind.CXCursor_ClassDecl,
        CXCursorKind.CXCursor_CXXMethod,
        CXCursorKind.CXCursor_Constructor,
        CXCursorKind.CXCursor_Destructor,
        CXCursorKind.CXCursor_FieldDecl,
        CXCursorKind.CXCursor_FunctionDecl,
        CXCursorKind.CXCursor_StructDecl,
        CXCursorKind.CXCursor_TypedefDecl,
        CXCursorKind.CXCursor_VarDecl,
        CXCursorKind.CXCursor_EnumDecl,
        CXCursorKind.CXCursor_EnumConstantDecl,
        CXCursorKind.CXCursor_ClassTemplate];
    private static readonly CXCursorKind[] inValidCursorKind = [
        CXCursorKind.CXCursor_FriendDecl,
        CXCursorKind.CXCursor_CompoundStmt];

    /// <summary>
    /// Explore the source code file using the Clang library
    /// </summary>
    /// <param name="codeFile"> Source code file </param>
    /// <returns></returns>
    protected override async Task Scan(string codeFile, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, Path.Combine(solutionPath, codeFile), clangBaseArgs, []);
        using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);
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
            await ExploreAstNode(cursor, ct: ct);
        }
        translationUnits.Remove(tu);
    }

    /// <summary>
    /// Recursively explore Abstract syntax tree (AST)
    /// </summary>
    /// <param name="cursor"> Ast node </param>
    /// <param name="nameSpace"> C++ code namespace </param>
    /// <returns> accessSpecifiers </returns>
    private async Task<string?> ExploreAstNode(Cursor cursor, string? nameSpace = null, string? accessSpecifiers = null, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return null;
        if (!CheckIsProjectFileAndNotScanned(cursor.Location) || inValidCursorKind.Contains(cursor.CursorKind))
            return null;

        if (cursor.CursorKind == CXCursorKind.CXCursor_CXXAccessSpecifier)
        {
            if (cursor is AccessSpecDecl accessSpecDecl)
                return accessSpecDecl.Access.ToString();
        }

        cursor.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
        cursor.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
        using CXString fileName = file.Name;
        string fullFileName = fileName.ToString();
        if (string.IsNullOrEmpty(fullFileName))
        {
            logger.LogWarning("Location-> File is Empty: {FullFileName}", fullFileName);
            return null;
        }
        Location fullLocation = new(fullFileName, startLine, startColumn, endLine, endColumn);
        Location location = new(getRelativePath(fullFileName), startLine, startColumn, endLine, endColumn)
        {
            SourceCode = new(ClangCore.GetSourceCode(fullLocation)),
            DisplayName = new(GetDisplayName(cursor, fullLocation).TrimStart().TrimEnd(' ', '{', '\n', '\r', '\t'))
        };
        logger.LogDebug("Location-> StartLine: {StartLine}, EndLine: {EndLine}, StartColumn: {StartColumn}, EndColumn: {EndColumn}, FileName: {FileName}", startLine, endLine, startColumn, endColumn, location.FilePath);

        if (validCursorKind.Contains(cursor.CursorKind))
        {
            using CXString cXType = cursor.Handle.Type.Spelling;

            if (cursor is Decl decl)
            {
                decl.CanonicalDecl.Extent.Start.GetExpansionLocation(out CXFile defineFIle, out uint defineStartLine, out uint defineStartColumn, out uint _);
                decl.CanonicalDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint defineEndLine, out uint defineEndColumn, out uint _);
                using CXString defineFileName = defineFIle.Name;
                Location fullDefineLocation = new(defineFileName.ToString(), defineStartLine, defineStartColumn, defineEndLine, defineEndColumn);
                Location defineLocation = new(getRelativePath(defineFileName.ToString()), defineStartLine, defineStartColumn, defineEndLine, defineEndColumn)
                {
                    SourceCode = new(ClangCore.GetSourceCode(fullDefineLocation)),
                    DisplayName = new(GetDisplayName(decl.CanonicalDecl, fullDefineLocation).TrimStart().TrimEnd(' ', '{', '\n', '\r', '\t'))
                };
                logger.LogDebug("DefineLocation-> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", defineStartLine, defineEndLine, defineFileName);
                if (defineLocation == location && !cursor.CursorChildren.Any(item => item.CursorKind == CXCursorKind.CXCursor_CompoundStmt))
                {
                    AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, accessSpecifiers, location, null);
                    await InsertNode(addNode, decl);
                }
                else
                {
                    AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, accessSpecifiers, defineLocation == location ? null : defineLocation, location);
                    Result result = await InsertorUpdateImplementationNode(addNode, decl, ct);
                    printErrorMessage(result);
                }
            }
            else
            {
                AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, accessSpecifiers, null, location);
                Result result = await InsertorUpdateImplementationNode(addNode, null, ct);
                printErrorMessage(result);
            }
        }

        if (cursor.CursorKind == CXCursorKind.CXCursor_Namespace)
            nameSpace = nameSpace is null ? cursor.Spelling : nameSpace + "::" + cursor.Spelling;

        if (cursor.CursorKind == CXCursorKind.CXCursor_ClassDecl)
            accessSpecifiers = "CX_CXXPrivate";
        else if (cursor.CursorKind == CXCursorKind.CXCursor_StructDecl)
            accessSpecifiers = "CX_CXXPublic";
        else if (cursor.CursorKind == CXCursorKind.CXCursor_EnumDecl)
            accessSpecifiers = "CX_CXXPublic";

        foreach (var child in cursor.CursorChildren)
        {
            if (ct.IsCancellationRequested)
                break;

            string? accessSpecifiersTag = await ExploreAstNode(child, nameSpace, accessSpecifiers, ct);
            if (accessSpecifiersTag is not null)
                accessSpecifiers = accessSpecifiersTag;
        }

        return null;
    }

    private async Task InsertNode(AddNode addNode, Decl? decl)
    {
        logger.LogDebug("Add Node-> ProjectId: {ProjectId}, CursorKindSpelling: {CursorKindSpelling}, Spelling: {Spelling}", projectId, addNode.CursorKindSpelling, addNode.Spelling);
        (Guid nodeId, bool isExist) = await cxxAggregate.AddDefineNodeAsync(addNode);
        if (decl is null || isExist || addNode.DefineLocation is null)
            return;

        // link class or struct member
        LinkMember(nodeId, decl);
    }

    private async Task<Result> InsertorUpdateImplementationNode(AddNode addNode, Decl? decl, CancellationToken ct = default)
    {
        Result<Guid?> result = await cxxAggregate.InsertorUpdateImplementationLocationAsync(addNode, ct);
        if (result.IsFailed)
            return result.ToResult();
        if (decl is null || result.Value is null)
            return Result.Ok();

        LinkMember(result.Value.Value, addNode.DefineLocation is not null ? decl.CanonicalDecl : decl);
        return Result.Ok();
    }

    private void LinkMember(Guid nodeId, Decl decl)
    {
        if (decl.LexicalDeclContext is not null && ((Decl)decl.LexicalDeclContext).CursorKind is
            CXCursorKind.CXCursor_ClassDecl or
            CXCursorKind.CXCursor_StructDecl or
            CXCursorKind.CXCursor_EnumDecl)
        {
            ((Decl)decl.LexicalDeclContext).Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
            ((Decl)decl.LexicalDeclContext).Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
            using CXString fileName = file.Name;
            InsertLinkMemberToBuffer(nodeId, fileName, startLine, startColumn, endLine, endColumn);
        }
        else if (decl.LexicalParentCursor is not null && ((Decl)decl.LexicalParentCursor).CursorKind is CXCursorKind.CXCursor_ClassTemplate)
        {
            ((Decl)decl.LexicalParentCursor).Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
            ((Decl)decl.LexicalParentCursor).Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
            using CXString fileName = file.Name;
            InsertLinkMemberToBuffer(nodeId, fileName, startLine, startColumn, endLine, endColumn);
        }
    }

    private void InsertLinkMemberToBuffer(Guid nodeId, CXString fileName, uint startLine, uint startColumn, uint endLine, uint endColumn)
    {
        Location classLocation = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);
        ConcurrentDictionary<Guid, IReadOnlyList<string>> buffer = memberBuffer.GetOrAdd(classLocation, _ => new ConcurrentDictionary<Guid, IReadOnlyList<string>>());
        buffer.GetOrAdd(nodeId, _ => projectConfig.ProjectDependencies);
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
