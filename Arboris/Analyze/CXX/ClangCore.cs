using Arboris.Aggregate;
using Arboris.Analyze.CXX.Clang;
using Arboris.Models;
using Arboris.Models.Analyze.CXX;
using ClangSharp;
using FluentResults;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Index = ClangSharp.Index;

namespace Arboris.Analyze.CXX;

public class ClangCore : IDisposable
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

    public ClangCore(Guid projectId, string solutionPath, CxxAggregate cxxAggregate, ILogger<ClangCore> logger, ProjectInfo projectConfig)
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
        ScanNode scanNode = new(logger, cxxAggregate, projectId, index, projectConfig, solutionPath, clangArgs, PrintErrorMessage, GetRelativePath, IsFilesScaned, translationUnits);
        await scanNode.Scan(codeFiles, ct);

        if (ct.IsCancellationRequested)
        {
            logger.LogWarning("Trigger CancellationRequested");
            return;
        }
        ScanLink scanLink = new(logger, cxxAggregate, projectId, index, projectConfig, solutionPath, clangArgs, PrintErrorMessage, GetRelativePath, translationUnits);
        await scanLink.Scan(codeFiles, ct);

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

    ~ClangCore()
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

public class ClangFactory(CxxAggregate cxxAggregate, ILogger<ClangCore> logger)
{
    public ClangCore Create(Guid projectId, string path, ProjectInfo projectConfig)
        => new(projectId, path, cxxAggregate, logger, projectConfig);
}
