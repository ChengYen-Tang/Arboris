using Arboris.Aggregate;
using Arboris.Models;
using Arboris.Models.Analyze.CXX;
using ClangSharp;
using ClangSharp.Interop;
using FluentResults;
using Microsoft.Extensions.Logging;
using Index = ClangSharp.Index;

namespace Arboris.Analyze.CXX.Clang;

internal class ScanLink(ILogger logger,
    CxxAggregate cxxAggregate, Guid projectId, Index index,
    ProjectInfo projectConfig, string solutionPath, Dictionary<string, bool> isFilesScaned,
    string[] clangBaseArgs, List<TranslationUnit> translationUnits,
    Action<Result> printErrorMessage, Func<string, string> getRelativePath)
    : ScanAbstract(logger, isFilesScaned, getRelativePath)
{
    private static readonly CXCursorKind[] validMemberCursorKind = [
        CXCursorKind.CXCursor_FieldDecl,
        CXCursorKind.CXCursor_CXXMethod,
        CXCursorKind.CXCursor_TypedefDecl,
        CXCursorKind.CXCursor_VarDecl];

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
            return;
        }
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

    private async Task LinkNodeDependency(Cursor cursor, Location? compoundStmtLocation = null, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return;
        if (!CheckIsProjectFileAndNotScanned(cursor.Location))
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
                decl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
                decl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
                using CXString fileName = file.Name;
                Location csLocation = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);
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

            refCursor.Referenced.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
            refCursor.Referenced.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
            using CXString fileName = file.Name;
            Location location = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);
            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;
            logger.LogDebug("    CXCursor_TypeRef -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, fileName);
            Result result = await cxxAggregate.LinkDependencyAsync(projectId, compoundStmtLocation, location);
            printErrorMessage(result);
        }
        else if (cursor.CursorKind == CXCursorKind.CXCursor_CallExpr)
        {
            if (cursor is not CallExpr callExpr)
                return;

            if (callExpr.CalleeDecl is null)
                return;

            callExpr.CalleeDecl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
            callExpr.CalleeDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
            using CXString fileName = file.Name;
            Location location = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);

            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;

            if (cursor.Spelling == "operator=" && cursor is CXXOperatorCallExpr)
            {
                logger.LogDebug("    CXCursor_CallExpr -> operator= -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
                Result result = await cxxAggregate.LinkDependencyCallExprOperatorEqualAsync(projectId, compoundStmtLocation, location);
                printErrorMessage(result);
            }
            else
            {
                logger.LogDebug("    CXCursor_CallExpr -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
                Result result = await cxxAggregate.LinkDependencyAsync(projectId, compoundStmtLocation, location);
                printErrorMessage(result);
            }
        }
        else if (cursor.CursorKind == CXCursorKind.CXCursor_MemberRefExpr)
        {
            if (cursor is not MemberExpr memberExpr)
                return;

            if (memberExpr.MemberDecl is null)
                return;

            memberExpr.MemberDecl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
            memberExpr.MemberDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
            using CXString fileName = file.Name;
            Location location = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);

            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;

            logger.LogDebug("    CXCursor_MemberRefExpr -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
            Result result = await cxxAggregate.LinkDependencyAsync(projectId, compoundStmtLocation, location);
            printErrorMessage(result);
        }
        else if (cursor.CursorKind == CXCursorKind.CXCursor_OverloadedDeclRef)
        {
            if (cursor is not OverloadedDeclRef overloadedDeclRef)
                return;

            if (overloadedDeclRef.OverloadedDecls is null)
                return;

            foreach (var extent in overloadedDeclRef.OverloadedDecls.Select(item => item.Extent))
            {
                extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
                extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
                using CXString fileName = file.Name;
                Location location = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);
                if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                    return;

                logger.LogDebug("    CXCursor_OverloadedDeclRef -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
                Result result = await cxxAggregate.LinkDependencyAsync(projectId, compoundStmtLocation, location);
                printErrorMessage(result);
            }
        }
        else if (cursor.CursorKind == CXCursorKind.CXCursor_DeclRefExpr)
        {
            if (cursor is not DeclRefExpr declRefExpr)
                return;
            if (declRefExpr.FoundDecl is null)
                return;

            declRefExpr.FoundDecl.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
            declRefExpr.FoundDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
            using CXString fileName = file.Name;
            Location location = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);

            if (!VerifyLocation(location) || !VerifyFromNodeOutOfSelfNode(compoundStmtLocation, location))
                return;

            logger.LogDebug("    CXCursor_DeclRefExpr -> StartLine: {StartLine}, EndLine: {EndLine}, FileName: {FileName}", startLine, endLine, location.FilePath);
            Result result = await cxxAggregate.LinkDependencyAsync(projectId, compoundStmtLocation, location);
            printErrorMessage(result);
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
        Location location = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);
        logger.LogDebug("Location-> StartLine: {StartLine}, EndLine: {EndLine}, StartColumn: {StartColumn}, EndColumn: {EndColumn}, FileName: {FileName}", startLine, endLine, startColumn, endColumn, location.FilePath);

        if (validMemberCursorKind.Contains(cursor.CursorKind) && cursor is Decl decl)
        {
            decl.CanonicalDecl.Extent.Start.GetExpansionLocation(out CXFile defineFIle, out uint defineStartLine, out uint defineStartColumn, out uint _);
            decl.CanonicalDecl.Extent.End.GetExpansionLocation(out CXFile _, out uint defineEndLine, out uint defineEndColumn, out uint _);
            using CXString defineFileName = defineFIle.Name;
            Location defineLocation = new(getRelativePath(defineFileName.ToString()), defineStartLine, defineStartColumn, defineEndLine, defineEndColumn);
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

            refCursor.Referenced.Extent.Start.GetExpansionLocation(out CXFile file, out uint startLine, out uint startColumn, out uint _);
            refCursor.Referenced.Extent.End.GetExpansionLocation(out CXFile _, out uint endLine, out uint endColumn, out uint _);
            using CXString fileName = file.Name;
            Location location = new(getRelativePath(fileName.ToString()), startLine, startColumn, endLine, endColumn);
            if (VerifyLocation(location))
                await cxxAggregate.LinkTypeAsync(projectId, defineLocation, location);
        }

        foreach (var child in cursor.CursorChildren)
        {
            if (ct.IsCancellationRequested)
                break;
            await ScanAndLinkNodeType(child, defineLocation, ct);
        }
    }

    private static bool VerifyLocation(Location location)
    => !(string.IsNullOrEmpty(location.FilePath) || location.StartLine == 0 || location.EndLine == 0);

    private static bool VerifyFromNodeOutOfSelfNode(Location selfLocation, Location fromLocation)
        => !(selfLocation.FilePath == fromLocation.FilePath && fromLocation.StartLine >= selfLocation.StartLine && fromLocation.EndLine <= selfLocation.EndLine);
}
