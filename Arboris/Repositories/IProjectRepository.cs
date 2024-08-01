using Arboris.Models;
using FluentResults;

namespace Arboris.Repositories;

public interface IProjectRepository
{
    Task<Guid> CreateProjectAsync(CreateProject createProject);
    Task<Result<GetProject>> GetProjectAsync(Guid id);
    Task<GetProject[]> GetProjectsAsync();
    Task<Result> DeleteProjectAsync(Guid id);
}
