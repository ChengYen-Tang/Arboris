﻿using Arboris.Models.Analyze.CXX;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using FluentResults;

namespace Arboris.Aggregate;

public class CxxAggregate(ICxxRepository nodeRepository)
{
    public async Task<(Guid id, bool isExist)> AddNodeAsync(AddNode addNode)
    {
        Result<Guid> checkNodeExistsResult = await nodeRepository.CheckNodeExists(addNode);
        if (checkNodeExistsResult.IsSuccess)
            return (checkNodeExistsResult.Value, true);
        return (await nodeRepository.AddNodeAsync(addNode), false);
    }

    public Task<Result<Node>> GetNodeFromDefineLocation(Guid projectId, string vcProjectName, Location location)
        => nodeRepository.GetNodeFromDefineLocation(projectId, vcProjectName, location);

    public Task<Result> UpdateNodeAsync(Node node)
        => nodeRepository.UpdateNodeAsync(node);

    public Task<Result> LinkMemberAsync(Guid projectId, string vcProjectName, Location classLocation, Guid memberId)
        => nodeRepository.LinkMemberAsync(projectId, vcProjectName, classLocation, memberId);

    public Task<Result> LinkDependencyAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location fromLocation)
        => nodeRepository.LinkDependencyAsync(projectId, vcProjectName, nodeLocation, fromLocation);

    public Task<Result> LinkDependencyCallExprOperatorEqualAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location fromLocation)
        => nodeRepository.LinkDependencyCallExprOperatorEqualAsync(projectId, vcProjectName, nodeLocation, fromLocation);

    public async Task<Result> LinkTypeAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location typeLocation, bool isImplementation)
    {
        Result<Node> nodeResult = await nodeRepository.GetNodeFromDefineLocation(projectId, vcProjectName, nodeLocation);
        if (nodeResult.IsFailed)
            return nodeResult.ToResult();

        if (nodeResult.Value.ImplementationLocation is not null && !isImplementation)
            return Result.Ok();

        return await nodeRepository.LinkTypeAsync(projectId, vcProjectName, nodeLocation, typeLocation);
    }

    public Task<Result<NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync(Guid projectId, string vcProjectName)
        => nodeRepository.GetDistinctClassAndStructNodeInfosAsync(projectId, vcProjectName);

    public Task<Result> MoveTypeDeclarationLinkAsync(Guid projectId, NodeInfo nodeInfo)
        => nodeRepository.MoveTypeDeclarationLinkAsync(projectId, nodeInfo);

    public Task<Result> RemoveTypeDeclarations(Guid projectId, string vcProjectName)
        => nodeRepository.RemoveTypeDeclarations(projectId, vcProjectName);

    public async Task<Result<OverallGraph>> GetOverallGraphAsync(Guid projectId)
    {
        Task<Result<OverallNode[]>> overallNodeTask = nodeRepository.GetOverallNodeAsync(projectId);
        Task<Result<OverallNodeMember[]>> overallNodeMemberTask = nodeRepository.GetOverallNodeMemberAsync(projectId);
        Task<Result<OverallNodeType[]>> overallNodeTypeTask = nodeRepository.GetOverallNodeTypeAsync(projectId);
        Task<Result<OverallNodeDependency[]>> overallNodeDependencyTask = nodeRepository.GetOverallNodeDependencyAsync(projectId);

        await Task.WhenAll(overallNodeTask, overallNodeMemberTask, overallNodeTypeTask, overallNodeDependencyTask);

        Result<OverallNode[]> overallNodeResult = overallNodeTask.Result;
        Result<OverallNodeMember[]> overallNodeMemberResult = overallNodeMemberTask.Result;
        Result<OverallNodeType[]> overallNodeTypeResult = overallNodeTypeTask.Result;
        Result<OverallNodeDependency[]> overallNodeDependencyResult = overallNodeDependencyTask.Result;

        if (overallNodeResult.IsFailed)
            return overallNodeResult.ToResult();
        if (overallNodeMemberResult.IsFailed)
            return overallNodeMemberResult.ToResult();
        if (overallNodeTypeResult.IsFailed)
            return overallNodeTypeResult.ToResult();
        if (overallNodeDependencyResult.IsFailed)
            return overallNodeDependencyResult.ToResult();

        return new OverallGraph
        {
            Nodes = overallNodeResult.Value,
            NodeMembers = overallNodeMemberResult.Value,
            NodeTypes = overallNodeTypeResult.Value,
            NodeDependencies = overallNodeDependencyResult.Value
        };
    }

    public async Task<Result> UpdateLLMDescriptionAsync(Guid id, string description)
        => await nodeRepository.UpdateLLMDescriptionAsync(id, description);

    public async Task<Result<ForDescriptionGraph>> GetGraphForDescription(Guid id)
    {
        Task<Result<ForDescriptionNode>> ForDescriptionNodeTask = nodeRepository.GetNodeForDescriptionAsync(id);
        Task<Result<OverViewNode[]>> NodeMembersTask = nodeRepository.GetNodeMembersAsync(id);
        Task<Result<OverViewNode[]>> NodeTypesTask = nodeRepository.GetNodeTypesAsync(id);
        Task<Result<OverViewNode[]>> NodeDependenciesTask = nodeRepository.GetNodeDependenciesAsync(id);

        await Task.WhenAll(ForDescriptionNodeTask, NodeMembersTask, NodeTypesTask, NodeDependenciesTask);

        Result<ForDescriptionNode> ForDescriptionNodeResult = ForDescriptionNodeTask.Result;
        Result<OverViewNode[]> NodeMembersResult = NodeMembersTask.Result;
        Result<OverViewNode[]> NodeTypesResult = NodeTypesTask.Result;
        Result<OverViewNode[]> NodeDependenciesResult = NodeDependenciesTask.Result;

        if (ForDescriptionNodeResult.IsFailed)
            return ForDescriptionNodeResult.ToResult();
        if (NodeMembersResult.IsFailed)
            return NodeMembersResult.ToResult();
        if (NodeTypesResult.IsFailed)
            return NodeTypesResult.ToResult();
        if (NodeDependenciesResult.IsFailed)
            return NodeDependenciesResult.ToResult();

        return new ForDescriptionGraph
        {
            Node = ForDescriptionNodeResult.Value,
            NodeMembers = NodeMembersResult.Value,
            NodeTypes = NodeTypesResult.Value,
            NodeDependencies = NodeDependenciesResult.Value
        };
    }

    public async Task<Result<ForUnitTestGraph>> GetGraphForUnitTest(Guid id)
    {
        Task<Result<OverViewNode>> ForUnitTestNodeTask = nodeRepository.GetForUnitTestNodeAsync(id);
        Task<Result<OverViewNode[]>> NodeMembersTask = nodeRepository.GetNodeMembersAsync(id);
        Task<Result<OverViewNode[]>> NodeTypesTask = nodeRepository.GetNodeTypesAsync(id);
        Task<Result<OverViewNode[]>> NodeDependenciesTask = nodeRepository.GetNodeDependenciesAsync(id);

        await Task.WhenAll(ForUnitTestNodeTask, NodeMembersTask, NodeTypesTask, NodeDependenciesTask);

        Result<OverViewNode> ForUnitTestNodeResult = ForUnitTestNodeTask.Result;
        Result<OverViewNode[]> NodeMembersResult = NodeMembersTask.Result;
        Result<OverViewNode[]> NodeTypesResult = NodeTypesTask.Result;
        Result<OverViewNode[]> NodeDependenciesResult = NodeDependenciesTask.Result;

        if (ForUnitTestNodeResult.IsFailed)
            return ForUnitTestNodeResult.ToResult();
        if (NodeMembersResult.IsFailed)
            return NodeMembersResult.ToResult();
        if (NodeTypesResult.IsFailed)
            return NodeTypesResult.ToResult();
        if (NodeDependenciesResult.IsFailed)
            return NodeDependenciesResult.ToResult();

        return new ForUnitTestGraph
        {
            Node = ForUnitTestNodeResult.Value,
            NodeMembers = NodeMembersResult.Value,
            NodeTypes = NodeTypesResult.Value,
            NodeDependencies = NodeDependenciesResult.Value
        };
    }

    public async Task<Result<ForUtServiceFuncInfo>> GetFuncInfoForUtService(Guid id)
    {
        Task<Result<Node>> nodeTask = nodeRepository.GetNode(id);
        Task<string?> classNameTask = nodeRepository.GetClassFromNodeAsync(id);
        await Task.WhenAll(nodeTask, classNameTask);

        Result<Node> nodeResult = nodeTask.Result;
        string? className = classNameTask.Result;

        if (nodeResult.IsFailed)
            return nodeResult.ToResult();

        string filePath = nodeResult.Value.ImplementationLocation is not null ? nodeResult.Value.ImplementationLocation.FilePath : nodeResult.Value.DefineLocation!.FilePath;
        return new ForUtServiceFuncInfo(filePath, nodeResult.Value.Spelling, nodeResult.Value.CxType, nodeResult.Value.NameSpace, className, nodeResult.Value.CursorKindSpelling);
    }

    public Task<Result> UpdateUserDescription(Guid projectId, string vcProjectName, string? nameSpace, string? className, string? spelling, string? cxType, string? description)
        => nodeRepository.UpdateUserDescription(projectId, vcProjectName, nameSpace, className, spelling, cxType, description);
}
