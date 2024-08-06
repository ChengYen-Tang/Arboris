﻿using Arboris.Aggregate;
using Arboris.Models.CXX;
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
    private readonly CXCursorKind[] validCursorKind = [
        CXCursorKind.CXCursor_ClassDecl,
        CXCursorKind.CXCursor_CXXMethod,
        CXCursorKind.CXCursor_Constructor,
        CXCursorKind.CXCursor_Destructor,
        CXCursorKind.CXCursor_FieldDecl,
        CXCursorKind.CXCursor_FunctionDecl,
        CXCursorKind.CXCursor_StructDecl,
        CXCursorKind.CXCursor_TypedefDecl];

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

    public async Task ScanNode()
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
                await LinkNode(cursor);
            }
        }
    }

    private async Task ScanAndInsertNode(Cursor cursor)
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
            if (cursor is Decl decl)
            {
                decl.CanonicalDecl.Extent.Start.GetExpansionLocation(out CXFile defineFIle, out uint defineStartLine, out uint _, out uint _);
                decl.CanonicalDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint defineEndLine, out uint _, out uint _);
                using CXString defineFileName = defineFIle.Name;
                Location defineLocation = new(defineFileName.ToString(), defineStartLine, defineEndLine);

                if (cursor.CursorKind == CXCursorKind.CXCursor_ClassDecl || defineLocation == location)
                {
                    AddNode addNode = new(projectId, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), location, null);
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
                AddNode addNode = new(projectId, cursor.CursorKindSpelling, cursor.Spelling, cXType.ToString(), null, location);
                await InsertNode(addNode, null);
            }
        }

        foreach (var child in cursor.CursorChildren)
            await ScanAndInsertNode(child);
    }

    private async Task LinkNode(Cursor cursor, Location? compoundStmtLocation = null)
    {
        if (!cursor.Location.IsFromMainFile)
            return;

        if (compoundStmtLocation is not null)
            await ScanDependency(cursor, compoundStmtLocation);

        foreach (var child in cursor.CursorChildren)
        {
            if (compoundStmtLocation is not null)
                await LinkNode(child, compoundStmtLocation);
            else if (cursor.CursorKind == CXCursorKind.CXCursor_CompoundStmt)
            {
                CompoundStmt stmt = (cursor as CompoundStmt)!;
                if (stmt.DeclContext is not Decl decl)
                    continue;
                decl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint _, out uint _);
                decl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint _, out uint _);
                using CXString fileName = file.Name;
                Location csLocation = new(fileName.ToString(), startLine, endLine);
                await LinkNode(child, csLocation);
            }
            else
                await LinkNode(child);
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
            //Trace.Assert(result.IsSuccess, "LinkDependencyTyprRefAsync failed");
        }
        if (cursor.CursorKind == CXCursorKind.CXCursor_CallExpr)
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
                //Trace.Assert(result.IsSuccess, "LinkDependencyCallExprOperatorEqualAsync failed");
            }
            else
            {
                logger.LogDebug("    CXCursor_CallExpr -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, fileName);
                Result result = await cxxAggregate.LinkDependencyAsync(compoundStmtLocation, location);
                PrintErrorMessage(result);
                //Trace.Assert(result.IsSuccess, "LinkDependencyAsync failed");
            }
        }
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
