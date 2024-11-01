using Arboris.Models.Analyze.CXX;
using Arboris.Models.Graph.CXX;
using FluentResults;

namespace Arboris.Repositories;

public interface ICxxRepository
{
    Task<Result<Guid>> CheckNodeExists(AddNode addNode);
    Task<Guid> AddNodeAsync(AddNode addNode);
    Task<Result<Node>> GetNodeFromDefineLocation(Guid projectId, string vcProjectName, Location location);
    Task<Result> UpdateNodeAsync(Node node);
    Task<Result> LinkMemberAsync(Guid projectId, string vcProjectName, Location classLocation, Guid memberId);
    Task<Result> LinkDependencyAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location fromLocation);
    Task<Result> LinkDependencyCallExprOperatorEqualAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location fromLocation);
    Task<Result> LinkTypeAsync(Guid projectId, string vcProjectName, Location nodeLocation, Location typeLocation);
    Task<Result<NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync(Guid projectId, string vcProjectName);
    Task<Result> MoveTypeDeclarationLinkAsync(Guid projectId, NodeInfo nodeInfo);
    Task<Result> RemoveTypeDeclarations(Guid projectId, string vcProjectName);
    Task<Result<OverallNode[]>> GetOverallNodeAsync(Guid projectId);
    Task<Result<OverallNodeMember[]>> GetOverallNodeMemberAsync(Guid projectId);
    Task<Result<OverallNodeType[]>> GetOverallNodeTypeAsync(Guid projectId);
    Task<Result<OverallNodeDependency[]>> GetOverallNodeDependencyAsync(Guid projectId);
    Task<Result> UpdateLLMDescriptionAsync(Guid id, string description);
    Task<Result<ForDescriptionNode>> GetNodeForDescriptionAsync(Guid nodeId);
    Task<Result<OverViewNode>> GetForUnitTestNodeAsync(Guid nodeId);
    Task<Result<OverViewNode[]>> GetNodeMembersAsync(Guid nodeId);
    Task<Result<OverViewNode[]>> GetNodeTypesAsync(Guid nodeId);
    Task<Result<OverViewNode[]>> GetNodeDependenciesAsync(Guid nodeId);
    Task<NodeInfoWithLocation[]> GetNodesFromProjectAsync(Guid projectId);
    Task<string?> GetClassFromNodeAsync(Guid nodeId);
    Task<Result<Node>> GetNode(Guid nodeId);
    Task<Result> UpdateUserDescription(Guid projectId, string vcProjectName, string? nameSpace, string? className, string? spelling, string? cxType, string? description);
}
