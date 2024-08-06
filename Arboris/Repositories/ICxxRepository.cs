using Arboris.Models.CXX;
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
}
