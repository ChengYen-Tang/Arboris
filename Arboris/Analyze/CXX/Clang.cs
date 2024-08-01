using Arboris.Aggregate;
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
        CXCursorKind.CXCursor_FunctionDecl];

    public Clang(Guid projectId, string projectPath, CxxAggregate cxxAggregate, ILogger<Clang> logger)
    {
        this.projectId = projectId;
        this.projectPath = projectPath;
        this.cxxAggregate = cxxAggregate;
        this.logger = logger;
        index = Index.Create(false, false);
        List<string> args = ["-std=c++14", "-xc++"];
        args.AddRange(Utils.GetDirectoriesWithFiles(projectPath, ["*.h", "*.H", "*.cpp", "*.CPP", "*.hpp", "*.HPP"]).Select(item => $"-I{item}"));
        clangArgs = [.. args];
    }

    public async Task ScanNode()
    {
        IReadOnlyList<string> headerFiles = Utils.GetFilesWithExtensions(projectPath, ["*.h"]);
        await ScanNode(headerFiles);
        IReadOnlyList<string> cppFiles = Utils.GetFilesWithExtensions(projectPath, ["*.cpp"]);
        await ScanNode(cppFiles);
        IReadOnlyList<string> hppFiles = Utils.GetFilesWithExtensions(projectPath, ["*.hpp"]);
        await ScanNode(hppFiles);
    }

    private async Task ScanNode(IReadOnlyList<string> headerFiles)
    {
        foreach (string headerFile in headerFiles)
        {
            CXTranslationUnit translationUnit = CXTranslationUnit.CreateFromSourceFile(index.Handle, headerFile, clangArgs, []);
            using TranslationUnit tu = TranslationUnit.GetOrCreate(translationUnit);

            foreach (var cursor in tu.TranslationUnitDecl.CursorChildren)
            {
                await InsertNode(cursor);
            }
        }
    }

    private async Task InsertNode(Cursor cursor)
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
            await InsertNode(child);
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
            Result result = await cxxAggregate.LinkMember(classLocation, nodeId);
            Trace.Assert(result.IsSuccess, "ClassLocation not found");
        }
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
