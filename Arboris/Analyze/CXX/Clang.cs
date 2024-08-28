using Arboris.Aggregate;
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
    private readonly CxxAggregate cxxAggregate;
    private bool disposedValue;
    private readonly Index index;
    private readonly string[] clangArgs;
    private static readonly CXCursorKind[] validCursorKind = [
        CXCursorKind.CXCursor_ClassDecl,
        CXCursorKind.CXCursor_CXXMethod,
        CXCursorKind.CXCursor_Constructor,
        CXCursorKind.CXCursor_Destructor,
        CXCursorKind.CXCursor_FieldDecl,
        CXCursorKind.CXCursor_FunctionDecl,
        CXCursorKind.CXCursor_StructDecl,
        CXCursorKind.CXCursor_TypedefDecl];
    private static readonly CXCursorKind[] validMemberCursorKind = [
        CXCursorKind.CXCursor_FieldDecl,
        CXCursorKind.CXCursor_CXXMethod];

    public Clang(Guid projectId, string projectPath, CxxAggregate cxxAggregate, ILogger<Clang> logger)
    {
        this.projectId = projectId;
        this.projectPath = projectPath;
        this.cxxAggregate = cxxAggregate;
        this.logger = logger;
        index = Index.Create(false, false);
        List<string> args = ["-std=c++14", "-xc++"];
        args.AddRange(Utils.GetDirectoriesWithFiles(projectPath, ["*.h", "*.cpp", "*.hpp"]).Select(item => $"-I{item}"));
        clangArgs = [.. args];
    }

    public async Task Scan()
    {
        IReadOnlyList<string> headerFiles = Utils.GetFilesWithExtensions(projectPath, ["*.h"]);
        IReadOnlyList<string> cppFiles = Utils.GetFilesWithExtensions(projectPath, ["*.cpp"]);
        IReadOnlyList<string> hppFiles = Utils.GetFilesWithExtensions(projectPath, ["*.hpp"]);

        await ScanNode(headerFiles);
        await ScanNode(cppFiles);
        await ScanNode(hppFiles);

        await ScanLink(headerFiles);
        await ScanLink(cppFiles);
        await ScanLink(hppFiles);

        await RemoveTypeDeclarations();
    }

    private async Task RemoveTypeDeclarations()
    {
        Result<NodeInfo[]> nodeInfosResult = await cxxAggregate.GetDistinctClassAndStructNodeInfosAsync();
        foreach (var nodeInfo in nodeInfosResult.Value)
        {
            Result result = await cxxAggregate.MoveTypeDeclarationLinkAsync(nodeInfo);
            Trace.Assert(result.IsSuccess, "MoveTypeDeclarationDepandencyAsync failed");
        }

        await cxxAggregate.RemoveTypeDeclarations();
    }

    private async Task ScanNode(IReadOnlyList<string> headerFiles)
    {
        foreach (string headerFile in headerFiles)
        {
            CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, headerFile, clangArgs, []);
            using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);

            foreach (var cursor in tu.TranslationUnitDecl.CursorChildren)
            {
                await ScanAndInsertNode(cursor);
            }
        }
    }

    private async Task ScanLink(IReadOnlyList<string> headerFiles)
    {
        foreach (string headerFile in headerFiles)
        {
            CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, headerFile, clangArgs, []);
            using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);

            foreach (var cursor in tu.TranslationUnitDecl.CursorChildren)
            {
                await LinkNodeDependency(cursor);
                await ScanAndLinkNodeType(cursor);
            }
        }
    }

    private async Task ScanAndInsertNode(Cursor cursor, string? nameSpace = null)
    {
        if (!cursor.Location.IsFromMainFile)
            return;

        cursor.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
        cursor.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
        using CXString fileName = file.Name;
        Location location = new(fileName.ToString(), startLine, endLine);
        logger.LogDebug("Location-> StartLine: {StartLine}, EndLine: {EndLine}, StartColumn: {StartColumn}, EndColumn: {EndColumn}, FileName: {FileName}", startLine, endLine, startColumn, endColumn, fileName);

        using CXString cXType = cursor.Handle.Type.Spelling;

        if (validCursorKind.Contains(cursor.CursorKind))
        {
            location.SourceCode = string.Join(Environment.NewLine, File.ReadLines(location.FilePath).Skip((int)startLine - 1).Take((int)endLine - (int)startLine + 1));
            location.CodeDefine = GetDisplayName(cursor, location).TrimStart().TrimEnd(' ', '{', '\n', '\r');
            if (cursor is Decl decl)
            {
                decl.CanonicalDecl.Extent.Start.GetExpansionLocation(out CXFile defineFIle, out uint defineStartLine, out uint _, out uint _);
                decl.CanonicalDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint defineEndLine, out uint _, out uint _);
                using CXString defineFileName = defineFIle.Name;
                Location defineLocation = new(defineFileName.ToString(), defineStartLine, defineEndLine);

                if (cursor.CursorKind == CXCursorKind.CXCursor_ClassDecl || cursor.CursorKind == CXCursorKind.CXCursor_StructDecl || defineLocation == location)
                {
                    AddNode addNode = new(projectId, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, location, null);
                    await InsertNode(addNode, decl);
                }
                else
                {
                    logger.LogDebug("DefineLocation-> StartLine: {StartLine}, EndLine: {EndLine}, FilePath: {FilePath}", defineStartLine, defineEndLine, defineFileName);
                    Result<Node> node = await cxxAggregate.GetNodeFromDefineLocation(defineLocation);
                    Trace.Assert(node.IsSuccess, "Node not found for DefineLocation");
                    node.Value.ImplementationLocation = location;
                    Result updateResult = await cxxAggregate.UpdateNodeAsync(node.Value);
                    Trace.Assert(updateResult.IsSuccess, "Update Node failed");
                }
            }
            else
            {
                AddNode addNode = new(projectId, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), nameSpace, null, location);
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
        Location location = new(fileName.ToString(), startLine, endLine);
        logger.LogDebug("Location-> StartLine: {StartLine}, EndLine: {EndLine}, StartColumn: {StartColumn}, EndColumn: {EndColumn}, FileName: {FileName}", startLine, endLine, startColumn, endColumn, fileName);

        if (validMemberCursorKind.Contains(cursor.CursorKind) && cursor is Decl decl)
        {
            decl.CanonicalDecl.Extent.Start.GetExpansionLocation(out CXFile defineFIle, out uint defineStartLine, out uint _, out uint _);
            decl.CanonicalDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint defineEndLine, out uint _, out uint _);
            using CXString defineFileName = defineFIle.Name;
            Location defineLocation = new(defineFileName.ToString(), defineStartLine, defineEndLine);
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
                Location csLocation = new(fileName.ToString(), startLine, endLine);
                await LinkNodeDependency(child, csLocation);
            }
            else
                await LinkNodeDependency(child);
        }
    }

    private async Task InsertNode(AddNode addNode, Decl? decl)
    {
        logger.LogDebug("Add Node-> ProjectId: {ProjectId}, CursorKindSpelling: {CursorKindSpelling}, Spelling: {Spelling}", projectId, addNode.CursorKindSpelling, addNode.Spelling);
        Guid nodeId = await cxxAggregate.AddNodeAsync(addNode);
        if (decl is null)
            return;
        if (decl.LexicalDeclContext is not null && ((Decl)decl.LexicalDeclContext).CursorKind == CXCursorKind.CXCursor_ClassDecl)
        {
            ((Decl)decl.LexicalDeclContext).Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
            ((Decl)decl.LexicalDeclContext).Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
            using CXString fileName = file.Name;
            Location classLocation = new(fileName.ToString(), startLine, endLine);
            Result result = await cxxAggregate.LinkMemberAsync(classLocation, nodeId);
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
            Location location = new(fileName.ToString(), startLine, endLine);
            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;
            logger.LogDebug("    CXCursor_TypeRef -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, fileName);
            Result result = await cxxAggregate.LinkDependencyAsync(compoundStmtLocation, location);
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
            Location location = new(fileName.ToString(), startLine, endLine);

            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;

            if (cursor.Spelling == "operator=" && cursor is CXXOperatorCallExpr)
            {
                logger.LogDebug("    CXCursor_CallExpr -> operator= -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, fileName);
                Result result = await cxxAggregate.LinkDependencyCallExprOperatorEqualAsync(compoundStmtLocation, location);
                PrintErrorMessage(result);
            }
            else
            {
                logger.LogDebug("    CXCursor_CallExpr -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, fileName);
                Result result = await cxxAggregate.LinkDependencyAsync(compoundStmtLocation, location);
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
            Location location = new(fileName.ToString(), startLine, endLine);

            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;

            logger.LogDebug("    CXCursor_MemberRefExpr -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, fileName);
            Result result = await cxxAggregate.LinkDependencyAsync(compoundStmtLocation, location);
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
                Location location = new(fileName.ToString(), startLine, endLine);
                if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                    return;

                logger.LogDebug("    CXCursor_OverloadedDeclRef -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, fileName);
                Result result = await cxxAggregate.LinkDependencyAsync(compoundStmtLocation, location);
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
            Location location = new(fileName.ToString(), startLine, endLine);
            if (VerifyLocation(location))
                await cxxAggregate.LinkTypeAsync(defineLocation, location, isImplementation);
        }

        foreach (var child in cursor.CursorChildren)
            await ScanAndLinkNodeType(child, defineLocation, isImplementation);
    }

    private static bool VerifyLocation(Location location)
        => !(string.IsNullOrEmpty(location.FilePath) || location.StartLine == 0 || location.EndLine == 0);

    private static bool VerifyFromNodeOutOfSelfNode(Location selfLocation, Location fromLocation)
        => !(selfLocation.FilePath == fromLocation.FilePath && fromLocation.StartLine >= selfLocation.StartLine && fromLocation.EndLine <= selfLocation.EndLine);

    private void PrintErrorMessage(Result result)
    {
        if (result.IsFailed)
            foreach (var item in result.Errors)
                logger.LogError("    {ErrorMessage}", item.Message);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                // TODO: dispose managed state (managed objects)
            }

            // TODO: free unmanaged resources (unmanaged objects) and override finalizer
            // TODO: set large fields to null
            index.Dispose();
            disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
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
    public Clang Create(Guid projectId, string path)
        => new(projectId, path, cxxAggregate, logger);
}
