using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.Models;
using Arboris.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.Repositories;

public class ProjectRepository(IDbContextFactory<ArborisDbContext> dbContextFactory) : IProjectRepository
{
    public async Task<Guid> CreateProjectAsync(CreateProject createProject)
    {
        Project project = new() { Name = createProject.Name };
        using ArborisDbContext db = await dbContextFactory.CreateDbContextAsync();
        await db.Projects.AddAsync(project);
        await db.SaveChangesAsync();
        return project.Id;
    }

    public async Task<Result> DeleteProjectAsync(Guid id)
    {
        using ArborisDbContext db = await dbContextFactory.CreateDbContextAsync();
        Project? project = await db.Projects.FindAsync(id);
        if (project is null)
            return Result.Fail("Project not found");
        db.Projects.Remove(project);
        await db.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<GetProject>> GetProjectAsync(Guid id)
    {
        using ArborisDbContext db = await dbContextFactory.CreateDbContextAsync();
        Project? project = await db.Projects.FindAsync(id);
        if (project is null)
            return Result.Fail<GetProject>("Project not found");
        return Result.Ok(new GetProject(project.Id, project.Name, project.CreateTime));
    }

    public async Task<GetProject[]> GetProjectsAsync()
    {
        using ArborisDbContext db = await dbContextFactory.CreateDbContextAsync();
        return await db.Projects
            .Select(item => new GetProject(item.Id, item.Name, item.CreateTime))
            .ToArrayAsync();
    }
}
