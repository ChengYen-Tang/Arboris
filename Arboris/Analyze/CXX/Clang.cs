using Arboris.Aggregate;
using Arboris.Models;
using Arboris.Models.Analyze.CXX;
using ClangSharp;
using ClangSharp.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Index = ClangSharp.Index;

namespace Arboris.Analyze.CXX;

public class Clang : IDisposable
{
    private readonly ILogger logger;
    private readonly CxxAggregate cxxAggregate;
    private readonly Dictionary<string, byte> IsFilesScaned;
    private readonly Guid projectId;
    private readonly Index index;
    private readonly List<TranslationUnit> translationUnits;
    private readonly ProjectInfo projectConfig;
    private readonly string solutionPath;
    private readonly string[] clangArgs;
    private readonly Uri projectUri;
    private bool disposedValue;

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
    private static readonly CXCursorKind[] validMemberCursorKind = [
        CXCursorKind.CXCursor_FieldDecl,
        CXCursorKind.CXCursor_CXXMethod];

    public Clang(Guid projectId, string solutionPath, CxxAggregate cxxAggregate, ILogger<Clang> logger, ProjectInfo projectConfig)
    {
        this.projectId = projectId;
        this.solutionPath = solutionPath;
        projectUri = new Uri(solutionPath);
        this.cxxAggregate = cxxAggregate;
        this.projectConfig = projectConfig;
        this.logger = logger;
        translationUnits = [];
        index = Index.Create(false, false);
        List<string> args = ["-std=c++14", "-xc++"];
        args.AddRange(projectConfig.AdditionalIncludeDirectories.Select(item => $"-I{Path.Combine(solutionPath, item)}"));
        args.Add($"-I{Path.Combine(solutionPath, projectConfig.SourcePath)}");
        clangArgs = [.. args];
        IsFilesScaned = projectConfig.SourceCodePath.Select(item => item.Replace('\\', '/')).ToDictionary(item => item, _ => default(byte));
    }

    public async Task Scan(CancellationToken ct = default)
    {
        IReadOnlyList<string> codeFiles = [.. IsFilesScaned.Keys];

        if (ct.IsCancellationRequested)
        {
            logger.LogWarning("Trigger CancellationRequested");
            return;
        }
        await ScanNode(codeFiles, ct);

        if (ct.IsCancellationRequested)
        {
            logger.LogWarning("Trigger CancellationRequested");
            return;
        }
        await ScanLink(codeFiles, ct);

        if (ct.IsCancellationRequested)
        {
            logger.LogWarning("Trigger CancellationRequested");
            return;
        }
        await RemoveTypeDeclarations(ct);
    }

    private async Task RemoveTypeDeclarations(CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
        {
            logger.LogWarning("Trigger CancellationRequested");
            return;
        }
        Result<NodeInfo[]> nodeInfosResult = await cxxAggregate.GetDistinctClassAndStructNodeInfosAsync(projectId, projectConfig.ProjectName);

        if (ct.IsCancellationRequested)
        {
            logger.LogWarning("Trigger CancellationRequested");
            return;
        }
        if (nodeInfosResult.IsFailed)
        {
            logger.LogWarning("GetDistinctClassAndStructNodeInfosAsync failed-> ProjectId: {ProjectId}, ProjectName: {ProjectName}, Error Message: {Message}", projectId, projectConfig.ProjectName, nodeInfosResult.Errors[0]);
            return;
        }
        logger.LogDebug("GetDistinctClassAndStructNodeInfosAsync-> ProjectId: {ProjectId}, ProjectName: {ProjectName}, NodeInfos Count: {Count}", projectId, projectConfig.ProjectName, nodeInfosResult.Value.Length);
        foreach (var nodeInfo in nodeInfosResult.Value)
        {
            if (ct.IsCancellationRequested)
                break;
            logger.LogDebug("MoveTypeDeclarationLinkAsync-> NodeId: {NodeId}, ProjectId: {ProjectId}, ProjectName: {ProjectName}", nodeInfo.Id, projectId, projectConfig.ProjectName);
            Result moveResult = await cxxAggregate.MoveTypeDeclarationLinkAsync(projectId, nodeInfo);
            if (moveResult.IsFailed)
                logger.LogWarning("MoveTypeDeclarationLinkAsync failed-> NodeId: {NodeId}, ProjectId: {ProjectId}, ProjectName: {ProjectName}, Error Message: {Message}", nodeInfo.Id, projectId, projectConfig.ProjectName, moveResult.Errors[0]);
            Debug.Assert(moveResult.IsSuccess, "MoveTypeDeclarationDepandencyAsync failed");
        }

        if (ct.IsCancellationRequested)
        {
            logger.LogWarning("Trigger CancellationRequested");
            return;
        }
        Result result = await cxxAggregate.RemoveTypeDeclarations(projectId, projectConfig.ProjectName);
        if (result.IsFailed)
            logger.LogWarning("RemoveTypeDeclarations failed-> ProjectId: {ProjectId}, ProjectName: {ProjectName}, Error Message: {Message}", projectId, projectConfig.ProjectName, result.Errors[0]);
    }

    private async Task ScanNode(IReadOnlyList<string> codeFiles, CancellationToken ct = default)
    {
        foreach (string headerFile in codeFiles)
        {
            if (ct.IsCancellationRequested)
                break;
            await ScanNode(headerFile, ct);
        }
    }

    #region ScanNode
    /// <summary>
    /// Explore the source code file using the Clang library
    /// </summary>
    /// <param name="codeFile"> Source code file </param>
    /// <returns></returns>
    private async Task ScanNode(string codeFile, CancellationToken ct = default)
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
                    await ScanNode(defineLocation.FilePath, ct);
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
    #endregion

    private async Task ScanLink(IReadOnlyList<string> codeFiles, CancellationToken ct = default)
    {
        foreach (string file in codeFiles)
        {
            string codeFile = Path.Combine(solutionPath, file);
            CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, codeFile, clangArgs, []);
            using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);
            if (tu is null)
            {
                if (translationUnit != null)
                    translationUnit.Dispose();
                continue;
            }
            if (ct.IsCancellationRequested)
                break;
            translationUnits.Add(tu);
            foreach (var cursor in tu.TranslationUnitDecl.CursorChildren)
            {
                if (ct.IsCancellationRequested)
                    break;
                await LinkNodeDependency(cursor, ct: ct);
                await ScanAndLinkNodeType(cursor, ct);
            }
            translationUnits.Remove(tu);
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

    private async Task LinkNodeDependency(Cursor cursor, Location? compoundStmtLocation = null, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        if (!cursor.Location.IsFromMainFile)
            return;

        if (compoundStmtLocation is not null)
            await ScanDependency(cursor, compoundStmtLocation, ct);

        foreach (var child in cursor.CursorChildren)
        {
            if (ct.IsCancellationRequested)
                break;
            if (compoundStmtLocation is not null)
                await LinkNodeDependency(child, compoundStmtLocation, ct);
            else if (cursor.CursorKind == CXCursorKind.CXCursor_CompoundStmt)
            {
                CompoundStmt stmt = (cursor as CompoundStmt)!;
                if (stmt.DeclContext is not Decl decl)
                    continue;
                decl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
                decl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
                using CXString fileName = file.Name;
                Location csLocation = new(GetRelativePath(fileName.ToString()), startLine, endLine);
                await LinkNodeDependency(child, csLocation, ct);
            }
            else
                await LinkNodeDependency(child, ct: ct);
        }
    }

    private async Task ScanDependency(Cursor cursor, Location compoundStmtLocation, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        if (cursor.CursorKind == CXCursorKind.CXCursor_DeclStmt)
            return;
        logger.LogDebug("ScanDependency-> CursorKind: {CursorKind}, Spelling: {Spelling}", cursor.CursorKind, cursor.Spelling);
        logger.LogDebug("  CompoundStmtLocation-> StartLine: {StartLine}, EndLine: {EndLine}, FilePath: {FilePath}", compoundStmtLocation.StartLine, compoundStmtLocation.EndLine, compoundStmtLocation.FilePath);
        if (cursor.CursorKind == CXCursorKind.CXCursor_TypeRef)
        {
            if (cursor is not Ref refCursor)
                return;

            refCursor.Referenced.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
            refCursor.Referenced.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
            using CXString fileName = file.Name;
            Location location = new(GetRelativePath(fileName.ToString()), startLine, endLine);
            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;
            logger.LogDebug("    CXCursor_TypeRef -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, fileName);
            Result result = await cxxAggregate.LinkDependencyAsync(projectId, projectConfig.ProjectName, compoundStmtLocation, location);
            PrintErrorMessage(result);
        }
        else if (cursor.CursorKind == CXCursorKind.CXCursor_CallExpr)
        {
            if (cursor is not CallExpr callExpr)
                return;

            if (callExpr.CalleeDecl is null)
                return;

            callExpr.CalleeDecl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
            callExpr.CalleeDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
            using CXString fileName = file.Name;
            Location location = new(GetRelativePath(fileName.ToString()), startLine, endLine);

            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;

            if (cursor.Spelling == "operator=" && cursor is CXXOperatorCallExpr)
            {
                logger.LogDebug("    CXCursor_CallExpr -> operator= -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
                Result result = await cxxAggregate.LinkDependencyCallExprOperatorEqualAsync(projectId, projectConfig.ProjectName, compoundStmtLocation, location);
                PrintErrorMessage(result);
            }
            else
            {
                logger.LogDebug("    CXCursor_CallExpr -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
                Result result = await cxxAggregate.LinkDependencyAsync(projectId, projectConfig.ProjectName, compoundStmtLocation, location);
                PrintErrorMessage(result);
            }
        }
        else if (cursor.CursorKind == CXCursorKind.CXCursor_MemberRefExpr)
        {
            if (cursor is not MemberExpr memberExpr)
                return;

            if (memberExpr.MemberDecl is null)
                return;

            memberExpr.MemberDecl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
            memberExpr.MemberDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
            using CXString fileName = file.Name;
            Location location = new(GetRelativePath(fileName.ToString()), startLine, endLine);

            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;

            logger.LogDebug("    CXCursor_MemberRefExpr -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
            Result result = await cxxAggregate.LinkDependencyAsync(projectId, projectConfig.ProjectName, compoundStmtLocation, location);
            PrintErrorMessage(result);
        }
        else if (cursor.CursorKind == CXCursorKind.CXCursor_OverloadedDeclRef)
        {
            if (cursor is not OverloadedDeclRef overloadedDeclRef)
                return;

            if (overloadedDeclRef.OverloadedDecls is null)
                return;

            foreach (var extent in overloadedDeclRef.OverloadedDecls.Select(item => item.Extent))
            {
                extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
                extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
                using CXString fileName = file.Name;
                Location location = new(GetRelativePath(fileName.ToString()), startLine, endLine);
                if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                    return;

                logger.LogDebug("    CXCursor_OverloadedDeclRef -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
                Result result = await cxxAggregate.LinkDependencyAsync(projectId, projectConfig.ProjectName, compoundStmtLocation, location);
                PrintErrorMessage(result);
            }
        }
    }

    private async Task ScanAndLinkNodeType(Cursor cursor, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        if (!cursor.Location.IsFromMainFile)
            return;

        cursor.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
        cursor.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
        using CXString fileName = file.Name;
        Location location = new(GetRelativePath(fileName.ToString()), startLine, endLine);
        logger.LogDebug("Location-> StartLine: {StartLine}, EndLine: {EndLine}, StartColumn: {StartColumn}, EndColumn: {EndColumn}, FileName: {FileName}", startLine, endLine, startColumn, endColumn, location.FilePath);

        if (validMemberCursorKind.Contains(cursor.CursorKind) && cursor is Decl decl)
        {
            decl.CanonicalDecl.Extent.Start.GetExpansionLocation(out CXFile defineFIle, out uint defineStartLine, out uint _, out uint _);
            decl.CanonicalDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint defineEndLine, out uint _, out uint _);
            using CXString defineFileName = defineFIle.Name;
            Location defineLocation = new(GetRelativePath(defineFileName.ToString()), defineStartLine, defineEndLine);
            await ScanAndLinkNodeType(cursor, defineLocation, ct);
        }

        foreach (var child in cursor.CursorChildren)
        {
            if (ct.IsCancellationRequested)
                break;
            await ScanAndLinkNodeType(child, ct);
        }
    }

    private async Task ScanAndLinkNodeType(Cursor cursor, Location defineLocation, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        if (cursor is CompoundStmt)
            return;

        if (cursor.CursorKind == CXCursorKind.CXCursor_TypeRef)
        {
            if (cursor is not Ref refCursor)
                return;

            refCursor.Referenced.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
            refCursor.Referenced.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
            using CXString fileName = file.Name;
            Location location = new(GetRelativePath(fileName.ToString()), startLine, endLine);
            if (VerifyLocation(location))
                await cxxAggregate.LinkTypeAsync(projectId, projectConfig.ProjectName, defineLocation, location);
        }

        foreach (var child in cursor.CursorChildren)
        {
            if (ct.IsCancellationRequested)
                break;
            await ScanAndLinkNodeType(child, defineLocation, ct);
        }
    }

    private string GetRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;
        string relativePath = Uri.UnescapeDataString(projectUri.MakeRelativeUri(new Uri(path)).ToString()).Replace('\\', '/');
        string lastSegment = projectUri.Segments[projectUri!.Segments.Length - 1];
        if (relativePath.StartsWith(lastSegment))
            return relativePath[(lastSegment.Length + 1)..];
        return relativePath;
    }

    private static bool VerifyLocation(Location location)
        => !(string.IsNullOrEmpty(location.FilePath) || location.StartLine == 0 || location.EndLine == 0);

    private static bool VerifyFromNodeOutOfSelfNode(Location selfLocation, Location fromLocation)
        => !(selfLocation.FilePath == fromLocation.FilePath && fromLocation.StartLine >= selfLocation.StartLine && fromLocation.EndLine <= selfLocation.EndLine);

    private void PrintErrorMessage(Result result)
    {
        if (result.IsFailed)
            foreach (var item in result.Errors)
                logger.LogWarning("    {ErrorMessage}", item.Message);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                foreach (var tu in translationUnits)
                    tu.Dispose();
                index.Dispose();
            }

            disposedValue = true;
        }
    }

    ~Clang()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public class ClangFactory(CxxAggregate cxxAggregate, ILogger<Clang> logger)
{
    public Clang Create(Guid projectId, string path, ProjectInfo projectConfig)
        => new(projectId, path, cxxAggregate, logger, projectConfig);
}
