using Arboris.Models.Analyze.CXX;
using Arboris.Models.Graph.CXX;
using FluentResults;

namespace Arboris.Repositories;

public interface ICxxRepository
{
    Task<Guid> AddNodeAsync(AddNode addNode);
    Task<Result<Node>> GetNodeFromDefineLocation(Location location);
    Task<Result> UpdateNodeAsync(Node node);
    Task<Result> LinkMemberAsync(Location classLocation, Guid memberId);
    Task<Result> LinkDependencyAsync(Location nodeLocation, Location fromLocation);
    Task<Result> LinkDependencyCallExprOperatorEqualAsync(Location nodeLocation, Location fromLocation);
    Task<Result> LinkTypeAsync(Location nodeLocation, Location typeLocation);
    Task<Result<NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync();
    Task<Result> MoveTypeDeclarationLinkAsync(NodeInfo nodeInfo);
    Task<Result> RemoveTypeDeclarations();
    Task<Result<OverallNode[]>> GetOverallNodeAsync(Guid projectId);
    Task<Result<OverallNodeMember[]>> GetOverallNodeMemberAsync(Guid projectId);
    Task<Result<OverallNodeType[]>> GetOverallNodeTypeAsync(Guid projectId);
    Task<Result<OverallNodeDependency[]>> GetOverallNodeDependencyAsync(Guid projectId);
    Task<Result> UpdateLLMDescriptionAsync(Guid id, string description);
    Task<Result<ForDescriptionNode>> GetForDescriptionNodeAsync(Guid nodeId);
    Task<Result<OverViewNode[]>> GetNodeMembersAsync(Guid nodeId);
    Task<Result<OverViewNode[]>> GetNodeTypesAsync(Guid nodeId);
    Task<Result<OverViewNode[]>> GetNodeDependenciesAsync(Guid nodeId);
}
