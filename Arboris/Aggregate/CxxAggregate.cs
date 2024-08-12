using Arboris.Models.CXX;
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

    public Task<Result> MoveTypeDeclarationTypeAsync(NodeInfo nodeInfo)
        => nodeRepository.MoveTypeDeclarationTypeAsync(nodeInfo);

    public Task<Result> RemoveTypeDeclarations()
        => nodeRepository.RemoveTypeDeclarations();
}
