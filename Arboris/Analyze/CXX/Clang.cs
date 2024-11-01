﻿using Arboris.Aggregate;
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
    private readonly Guid projectId;
    private readonly string projectPath;
    private readonly Uri projectUri;
    private readonly CxxAggregate cxxAggregate;
    private bool disposedValue;
    private readonly Index index;
    private readonly string[] clangArgs;
    private readonly ProjectConfig projectConfig;
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
    private readonly Dictionary<string, byte> IsFilesScaned;
    private readonly List<TranslationUnit> translationUnits;

    public Clang(Guid projectId, string projectPath, CxxAggregate cxxAggregate, ILogger<Clang> logger, ProjectConfig projectConfig)
    {
        this.projectId = projectId;
        this.projectPath = projectPath;
        projectUri = new Uri(projectPath);
        this.cxxAggregate = cxxAggregate;
        this.projectConfig = projectConfig;
        this.logger = logger;
        translationUnits = [];
        index = Index.Create(false, false);
        List<string> args = ["-std=c++14", "-xc++"];
        args.AddRange(projectConfig.IncludeDirectories.Select(item => $"-I{Path.Combine(projectPath, item)}"));
        args.Add($"-I{Path.Combine(projectPath, projectConfig.SourcePath)}");
        clangArgs = [.. args];
        IsFilesScaned = projectConfig.SourceCodePath.Select(item => item.Replace('\\', '/')).ToDictionary(item => item, _ => default(byte));
    }

    public async Task Scan()
    {
        IReadOnlyList<string> codeFiles = [.. IsFilesScaned.Keys];

        await ScanNode(codeFiles);
        await ScanLink(codeFiles);

        await RemoveTypeDeclarations();
    }

    private async Task RemoveTypeDeclarations()
    {
        Result<NodeInfo[]> nodeInfosResult = await cxxAggregate.GetDistinctClassAndStructNodeInfosAsync(projectId, projectConfig.ProjectName);
        foreach (var nodeInfo in nodeInfosResult.Value)
        {
            Result result = await cxxAggregate.MoveTypeDeclarationLinkAsync(projectId, nodeInfo);
            Trace.Assert(result.IsSuccess, "MoveTypeDeclarationDepandencyAsync failed");
        }

        await cxxAggregate.RemoveTypeDeclarations(projectId, projectConfig.ProjectName);
    }

    private async Task ScanNode(IReadOnlyList<string> codeFiles)
    {
        foreach (string headerFile in codeFiles)
        {
            await ScanNode(headerFile);
        }
    }

    private async Task ScanNode(string codeFile)
    {
        if (!IsFilesScaned.TryGetValue(codeFile, out byte value) || value == 1 || value == 2)
            return;

        IsFilesScaned[codeFile] = 1;
        CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, Path.Combine(projectPath, codeFile), clangArgs, []);
        using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);
        if (tu is null)
        {
            if (translationUnit != null)
                translationUnit.Dispose();
            return;
        }
        translationUnits.Add(tu);
        foreach (var cursor in tu.TranslationUnitDecl.CursorChildren)
        {
            await ScanAndInsertNode(cursor);
        }
        translationUnits.Remove(tu);
        IsFilesScaned[codeFile] = 2;
    }

    private async Task ScanLink(IReadOnlyList<string> codeFiles)
    {
        foreach (string file in codeFiles)
        {
            string codeFile = Path.Combine(projectPath, file);
            CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, codeFile, clangArgs, []);
            using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);
            if (tu is null)
            {
                if (translationUnit != null)
                    translationUnit.Dispose();
                return;
            }
            translationUnits.Add(tu);
            foreach (var cursor in tu.TranslationUnitDecl.CursorChildren)
            {
                await LinkNodeDependency(cursor);
                await ScanAndLinkNodeType(cursor);
            }
            translationUnits.Remove(tu);
        }
    }

    private async Task ScanAndInsertNode(Cursor cursor, string? nameSpace = null)
    {
        if (!cursor.Location.IsFromMainFile || inValidCursorKind.Contains(cursor.CursorKind))
            return;

        cursor.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
        cursor.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
        using CXString fileName = file.Name;
        string fullFileName = fileName.ToString();
        Location fullLocation = new(fullFileName, startLine, endLine);
        Location location = new(GetRelativePath(fullFileName), startLine, endLine);
        logger.LogDebug("Location-> StartLine: {StartLine}, EndLine: {EndLine}, StartColumn: {StartColumn}, EndColumn: {EndColumn}, FileName: {FileName}", startLine, endLine, startColumn, endColumn, location.FilePath);
        using CXString cXType = cursor.Handle.Type.Spelling;

        if (validCursorKind.Contains(cursor.CursorKind))
        {
            location.SourceCode = string.Join(Environment.NewLine, File.ReadLines(fullLocation.FilePath).Skip((int)startLine - 1).Take((int)endLine - (int)startLine + 1));
            location.DisplayName = GetDisplayName(cursor, fullLocation).TrimStart().TrimEnd(' ', '{', '\n', '\r', '\t');
            if (cursor is Decl decl)
            {
                decl.CanonicalDecl.Extent.Start.GetExpansionLocation(out CXFile defineFIle, out uint defineStartLine, out uint _, out uint _);
                decl.CanonicalDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint defineEndLine, out uint _, out uint _);
                using CXString defineFileName = defineFIle.Name;
                Location defineLocation = new(GetRelativePath(defineFileName.ToString()), defineStartLine, defineEndLine);

                if (cursor.CursorKind == CXCursorKind.CXCursor_ClassDecl || cursor.CursorKind == CXCursorKind.CXCursor_StructDecl || defineLocation == location)
                {
                    AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, location, null);
                    await InsertNode(addNode, decl);
                }
                else
                {
                    await ScanNode(defineLocation.FilePath);
                    logger.LogDebug("DefineLocation-> StartLine: {StartLine}, EndLine: {EndLine}, FilePath: {FilePath}", defineStartLine, defineEndLine, defineFileName);
                    Result<Node> node = await cxxAggregate.GetNodeFromDefineLocation(projectId, projectConfig.SourcePath, defineLocation);
                    //Trace.Assert(node.IsSuccess, "Node not found for DefineLocation");
                    if (node.IsSuccess)
                    {
                        node.Value.ImplementationLocation = location;
                        Result updateResult = await cxxAggregate.UpdateNodeAsync(node.Value);
                        Trace.Assert(updateResult.IsSuccess, "Update Node failed");
                    }
                    else
                    {
                        AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, location, null);
                        await InsertNode(addNode, decl);
                    }
                }
            }
            else
            {
                AddNode addNode = new(projectId, projectConfig.ProjectName, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, null, location);
                await InsertNode(addNode, null);
            }
        }

        if (cursor.CursorKind == CXCursorKind.CXCursor_Namespace)
            nameSpace = nameSpace is null ? cursor.Spelling : nameSpace + "::" + cursor.Spelling;

        foreach (var child in cursor.CursorChildren)
            await ScanAndInsertNode(child, nameSpace);
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
                    child.Extent.Start.GetExpansionLocation(out CXFile _, out uint startLine, out uint _, out uint _);
                    child.Extent.End.GetExpansionLocation(out CXFile _, out uint _, out uint _, out uint _);
                    return string.Join(Environment.NewLine, File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).Take((int)startLine - 1 - (int)rootLocation.StartLine + 1));
                }
            }
            return string.Join(Environment.NewLine, File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).Take((int)rootLocation.EndLine - (int)rootLocation.StartLine + 1));
        }
        else
            return string.Join(Environment.NewLine, File.ReadLines(rootLocation.FilePath).Skip((int)rootLocation.StartLine - 1).Take((int)rootLocation.EndLine - (int)rootLocation.StartLine + 1));
    }

    private async Task ScanAndLinkNodeType(Cursor cursor)
    {
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
            await ScanAndLinkNodeType(cursor, defineLocation, defineLocation != location);
        }

        foreach (var child in cursor.CursorChildren)
            await ScanAndLinkNodeType(child);
    }

    private async Task LinkNodeDependency(Cursor cursor, Location? compoundStmtLocation = null)
    {
        if (!cursor.Location.IsFromMainFile)
            return;

        if (compoundStmtLocation is not null)
            await ScanDependency(cursor, compoundStmtLocation);

        foreach (var child in cursor.CursorChildren)
        {
            if (compoundStmtLocation is not null)
                await LinkNodeDependency(child, compoundStmtLocation);
            else if (cursor.CursorKind == CXCursorKind.CXCursor_CompoundStmt)
            {
                CompoundStmt stmt = (cursor as CompoundStmt)!;
                if (stmt.DeclContext is not Decl decl)
                    continue;
                decl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
                decl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
                using CXString fileName = file.Name;
                Location csLocation = new(GetRelativePath(fileName.ToString()), startLine, endLine);
                await LinkNodeDependency(child, csLocation);
            }
            else
                await LinkNodeDependency(child);
        }
    }

    private async Task InsertNode(AddNode addNode, Decl? decl)
    {
        logger.LogDebug("Add Node-> ProjectId: {ProjectId}, CursorKindSpelling: {CursorKindSpelling}, Spelling: {Spelling}", projectId, addNode.CursorKindSpelling, addNode.Spelling);
        (Guid nodeId, bool isExist) = await cxxAggregate.AddNodeAsync(addNode);
        if (decl is null || isExist)
            return;
        if (decl.LexicalDeclContext is not null && ((Decl)decl.LexicalDeclContext).CursorKind is CXCursorKind.CXCursor_ClassDecl or CXCursorKind.CXCursor_StructDecl)
        {
            ((Decl)decl.LexicalDeclContext).Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
            ((Decl)decl.LexicalDeclContext).Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
            using CXString fileName = file.Name;
            Location classLocation = new(GetRelativePath(fileName.ToString()), startLine, endLine);
            Result result = await cxxAggregate.LinkMemberAsync(projectId, projectConfig.ProjectName, classLocation, nodeId);
            Trace.Assert(result.IsSuccess, "ClassLocation not found");
        }
    }

    private async Task ScanDependency(Cursor cursor, Location compoundStmtLocation)
    {
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

    private async Task ScanAndLinkNodeType(Cursor cursor, Location defineLocation, bool isImplementation = false)
    {
        if (isImplementation && cursor is CompoundStmt)
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
                await cxxAggregate.LinkTypeAsync(projectId, projectConfig.ProjectName, defineLocation, location, isImplementation);
        }

        foreach (var child in cursor.CursorChildren)
            await ScanAndLinkNodeType(child, defineLocation, isImplementation);
    }

    private string GetRelativePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;
        string relativePath = Uri.UnescapeDataString(projectUri.MakeRelativeUri(new Uri(path)).ToString()).Replace('\\', '/');
        string lastSegment = projectUri.Segments[projectUri!.Segments.Length - 1];
        if (relativePath.StartsWith(lastSegment))
            return relativePath.Substring(lastSegment.Length + 1);
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
    public Clang Create(Guid projectId, string path, ProjectConfig projectConfig)
        => new(projectId, path, cxxAggregate, logger, projectConfig);
}
