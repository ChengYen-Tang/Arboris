using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.Models;
using Arboris.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.Repositories;

public class ProjectRepository(IDbContextFactory<ArborisDbContext> dbContextFactory) : IProjectRepository
{
    public async Task<Result> CreateProjectAsync(Guid Id)
    {
        using ArborisDbContext db = await dbContextFactory.CreateDbContextAsync();
        if (await db.Projects.AnyAsync(item => item.Id == Id))
            return Result.Fail("Project already exists");
        Project project = new() { Id = Id };
        await db.Projects.AddAsync(project);
        await db.SaveChangesAsync();
        return Result.Ok();
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
        return Result.Ok(new GetProject(project.Id, project.CreateTime));
    }

    public async Task<GetProject[]> GetProjectsAsync()
    {
        using ArborisDbContext db = await dbContextFactory.CreateDbContextAsync();
        return await db.Projects
            .Select(item => new GetProject(item.Id, item.CreateTime))
            .ToArrayAsync();
    }
}
