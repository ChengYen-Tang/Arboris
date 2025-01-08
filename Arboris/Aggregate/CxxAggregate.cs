using Arboris.Models.Analyze.CXX;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using FluentResults;
using System.Text;

namespace Arboris.Aggregate;

public class CxxAggregate(ICxxRepository nodeRepository)
{
    /// <summary>
    /// Add a new node(Define location) to the database
    /// </summary>
    /// <param name="addNode"> AddNode dto </param>
    /// <returns>
    /// 1. Node id
    /// 2. Is node exists
    /// </returns>
    public async Task<(Guid id, bool isExist)> AddDefineNodeAsync(AddNode addNode)
    {
        Result<Guid> checkNodeExistsResult = await nodeRepository.CheckDefineNodeExistsAsync(addNode);
        if (checkNodeExistsResult.IsSuccess)
            return (checkNodeExistsResult.Value, true);
        return (await nodeRepository.AddNodeAsync(addNode), false);
    }

    /// <summary>
    /// Link member Node to class node
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="classLocation"> Location of class or struct node </param>
    /// <param name="memberId"> Member node id </param>
    /// <returns></returns>
    public Task<Result> LinkMemberAsync(Guid projectId, string vcProjectName, Location classLocation, Guid memberId)
        => nodeRepository.LinkMemberAsync(projectId, vcProjectName, classLocation, memberId);

    /// <summary>
    /// Find node dependency and link to the node
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="nodeLocation"> Source node location </param>
    /// <param name="fromLocation"> Uesd node location </param>
    /// <returns></returns>
    public Task<Result> LinkDependencyAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location fromLocation)
        => nodeRepository.LinkDependencyAsync(projectId, vcProjectName, nodeLocation, fromLocation);

    /// <summary>
    /// Find node dependency and link to the node
    /// Because clang returns features of operator= under certain conditions that do not meet our requirements.
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="nodeLocation"> Source node location </param>
    /// <param name="fromLocation"> Uesd node location </param>
    /// <returns></returns>
    public Task<Result> LinkDependencyCallExprOperatorEqualAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location fromLocation)
        => nodeRepository.LinkDependencyCallExprOperatorEqualAsync(projectId, vcProjectName, nodeLocation, fromLocation);

    /// <summary>
    /// Find node type and link to the node
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="nodeLocation"> Source node location </param>
    /// <param name="typeLocation"> Type node location </param>
    /// <returns></returns>
    public async Task<Result> LinkTypeAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location typeLocation)
    {
        Result<Node> nodeResult = await nodeRepository.GetNodeFromDefineLocationAsync(projectId, vcProjectName, nodeLocation);
        if (nodeResult.IsFailed)
            return nodeResult.ToResult();

        return await nodeRepository.LinkTypeAsync(projectId, vcProjectName, nodeLocation, typeLocation);
    }

    /// <summary>
    /// Get node infos, only class and struct
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <returns></returns>
    public Task<Result<NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync(Guid projectId, string vcProjectName)
        => nodeRepository.GetDistinctClassAndStructNodeInfosAsync(projectId, vcProjectName);

    public Task<Result> MoveTypeDeclarationLinkAsync(Guid projectId, NodeInfo nodeInfo)
        => nodeRepository.MoveTypeDeclarationLinkAsync(projectId, nodeInfo);

    /// <summary>
    /// Remove type declarations
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <returns></returns>
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

    /// <summary>
    /// Update the description of the node, whith the description generated by the LLM
    /// </summary>
    /// <param name="id"> Node id </param>
    /// <param name="description"> Description generated by the LLM </param>
    /// <returns></returns>
    public async Task<Result> UpdateLLMDescriptionAsync(Guid id, string description)
        => await nodeRepository.UpdateLLMDescriptionAsync(id, description);

    public async Task<Result<NodeOtherInfoWithLocation>> GetNodeOtherInfoAsync(Guid nodeId)
    {
        Result<NodeInfoWithLocation> nodeResult = await nodeRepository.GetNodeFromNodeIdAsync(nodeId);
        if (nodeResult.IsFailed)
            return nodeResult.ToResult();
        NodeInfoWithLocation node = nodeResult.Value;
        string? className = nodeRepository.GetClassFromNodeAsync(node.Id).Result;
        StringBuilder sb = new();
        if (!string.IsNullOrEmpty(node.NameSpace))
            sb.Append(node.NameSpace).Append("::");
        if (!string.IsNullOrEmpty(className))
            sb.Append(className).Append("::");
        sb.Append(node.Spelling);

        List<string> relativePaths = [];

        if (node.DefineLocation is not null)
            relativePaths.Add(node.DefineLocation!.FilePath);

        if (node.ImplementationLocation is not null)
            relativePaths.Add(node.ImplementationLocation!.FilePath);
        return new NodeOtherInfoWithLocation(node.VcProjectName, sb.ToString(), node.Spelling, node.CxType, className, node.NameSpace, node.IncludeStrings, [.. relativePaths]);
    }

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
        Task<Result<Node>> nodeTask = nodeRepository.GetNodeAsync(id);
        Task<string?> classNameTask = nodeRepository.GetClassFromNodeAsync(id);
        await Task.WhenAll(nodeTask, classNameTask);

        Result<Node> nodeResult = nodeTask.Result;
        string? className = classNameTask.Result;

        if (nodeResult.IsFailed)
            return nodeResult.ToResult();

        string filePath = nodeResult.Value.ImplementationLocation is not null ? nodeResult.Value.ImplementationLocation.FilePath : nodeResult.Value.DefineLocation!.FilePath;
        return new ForUtServiceFuncInfo(filePath, nodeResult.Value.Spelling, nodeResult.Value.CxType, nodeResult.Value.NameSpace, className, nodeResult.Value.CursorKindSpelling);
    }

    public async Task<Result<ForCompileDto>> GetForCompileDtoAsync(Guid id)
    {
        Result<Node> node = await nodeRepository.GetNodeAsync(id);
        if (node.IsFailed)
            return node.ToResult();
        string filePath = node.Value.ImplementationLocation is not null ? node.Value.ImplementationLocation.FilePath : node.Value.DefineLocation!.FilePath;
        return new ForCompileDto(filePath, node.Value.IncludeStrings);
    }

    public async Task<Result<ForGenerateCodeDto>> GetForGenerateCodeDtoAsync(Guid id)
    {
        Result<Node> node = await nodeRepository.GetNodeAsync(id);
        if (node.IsFailed)
            return node.ToResult();
        string filePath = node.Value.ImplementationLocation is not null ? node.Value.ImplementationLocation.FilePath : node.Value.DefineLocation!.FilePath;
        List<string> relativePaths = [];

        if (node.Value.DefineLocation is not null)
            relativePaths.Add(node.Value.DefineLocation!.FilePath);

        if (node.Value.ImplementationLocation is not null)
            relativePaths.Add(node.Value.ImplementationLocation!.FilePath);
        return new ForGenerateCodeDto(node.Value.Spelling!, filePath, [.. relativePaths]);
    }

    /// <summary>
    /// Update the user description of the node
    /// </summary>
    /// <param name="projectId"> Project id </param>
    /// <param name="vcProjectName"> Visual studio project name </param>
    /// <param name="nameSpace"></param>
    /// <param name="className"></param>
    /// <param name="spelling"></param>
    /// <param name="cxType"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public Task<Result> UpdateUserDescriptionAsync(Guid projectId, string vcProjectName, string? nameSpace, string? className, string? spelling, string? cxType, string? description)
        => nodeRepository.UpdateUserDescriptionAsync(projectId, vcProjectName, nameSpace, className, spelling, cxType, description);

    public async Task<Result> InsertorUpdateImplementationLocationAsync(AddNode addNode, IReadOnlySet<string>? includeStrings, CancellationToken ct = default)
    {
        if (ct.IsCancellationRequested)
            return Result.Fail("Trigger CancellationRequested");
        if (addNode.DefineLocation is not null)
        {
            Result<Node> node = await nodeRepository.GetNodeFromDefineLocationAsync(addNode.ProjectId, addNode.VcProjectName, addNode.DefineLocation);
            if (node.IsSuccess)
            {
                Result UpdateResulr = await nodeRepository.UpdateNodeLocationAsync(new(node.Value.Id, node.Value.DefineLocation, addNode.ImplementationLocation));
                if (UpdateResulr.IsFailed)
                    return UpdateResulr;
                node.Value.IncludeStrings = includeStrings;
                UpdateResulr = await nodeRepository.UpdateNodeAsync(node.Value);
                if (UpdateResulr.IsFailed)
                    return UpdateResulr;
                return Result.Ok();
            }
        }
        bool isExists = await nodeRepository.CheckImplementationNodeExistsAsync(addNode);
        if (isExists)
            return Result.Ok();
        await nodeRepository.AddNodeAsync(addNode, includeStrings);
        return Result.Ok();
    }
}
