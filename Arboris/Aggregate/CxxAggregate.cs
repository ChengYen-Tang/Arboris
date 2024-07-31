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
}
