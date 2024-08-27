using Arboris.Models.Analyze.CXX;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using FluentResults;

namespace Arboris.Aggregate;

public class CxxAggregate(ICxxRepository nodeRepository)
{
    public Task<Guid> AddNodeAsync(AddNode addNode)
        => nodeRepository.AddNodeAsync(addNode);

    public Task<Result<Node>> GetNodeFromDefineLocation(Location location)
        => nodeRepository.GetNodeFromDefineLocation(location);

    public Task<Result> UpdateNodeAsync(Node node)
        => nodeRepository.UpdateNodeAsync(node);

    public Task<Result> LinkMemberAsync(Location classLocation, Guid memberId)
        => nodeRepository.LinkMemberAsync(classLocation, memberId);

    public Task<Result> LinkDependencyAsync(Location nodeLocation, Location fromLocation)
        => nodeRepository.LinkDependencyAsync(nodeLocation, fromLocation);

    public Task<Result> LinkDependencyCallExprOperatorEqualAsync(Location nodeLocation, Location fromLocation)
        => nodeRepository.LinkDependencyCallExprOperatorEqualAsync(nodeLocation, fromLocation);

    public async Task<Result> LinkTypeAsync(Location nodeLocation, Location typeLocation, bool isImplementation)
    {
        Result<Node> nodeResult = await nodeRepository.GetNodeFromDefineLocation(nodeLocation);
        if (nodeResult.IsFailed)
            return nodeResult.ToResult();

        if (nodeResult.Value.ImplementationLocation is not null && !isImplementation)
            return Result.Ok();

        return await nodeRepository.LinkTypeAsync(nodeLocation, typeLocation);
    }

    public Task<Result<NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync()
        => nodeRepository.GetDistinctClassAndStructNodeInfosAsync();

    public Task<Result> MoveTypeDeclarationLinkAsync(NodeInfo nodeInfo)
        => nodeRepository.MoveTypeDeclarationLinkAsync(nodeInfo);

    public Task<Result> RemoveTypeDeclarations()
        => nodeRepository.RemoveTypeDeclarations();

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
        Task<Result<ForUnitTestNode>> ForUnitTestNodeTask = nodeRepository.GetForUnitTestNodeAsync(id);
        Task<Result<OverViewNode[]>> NodeMembersTask = nodeRepository.GetNodeMembersAsync(id);
        Task<Result<OverViewNode[]>> NodeTypesTask = nodeRepository.GetNodeTypesAsync(id);
        Task<Result<OverViewNode[]>> NodeDependenciesTask = nodeRepository.GetNodeDependenciesAsync(id);

        await Task.WhenAll(ForUnitTestNodeTask, NodeMembersTask, NodeTypesTask, NodeDependenciesTask);

        Result<ForUnitTestNode> ForUnitTestNodeResult = ForUnitTestNodeTask.Result;
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
}
