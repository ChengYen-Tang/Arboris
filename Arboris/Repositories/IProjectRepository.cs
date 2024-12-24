using Arboris.Models;
using FluentResults;

namespace Arboris.Repositories;

public interface IProjectRepository
{
    Task<Result> CreateProjectAsync(Guid Id, string solutionName);
    Task<Result<GetProject>> GetProjectAsync(Guid id);
    Task<GetProject[]> GetProjectsAsync();
    Task<Result> DeleteProjectAsync(Guid id);
    Task DeleteTooOldProjectAsync(int days);
}
