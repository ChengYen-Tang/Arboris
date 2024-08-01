using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.Repositories;

public class CxxRepository(IDbContextFactory<ArborisDbContext> dbContextFactory) : ICxxRepository
{
    public async Task<Guid> AddNodeAsync(Models.CXX.AddNode addNode)
    {
        DefineLocation? defineLocation = null;
        ImplementationLocation? implementationLocation = null;

        if (addNode.DefineLocation is not null)
            defineLocation = new()
            {
                FilePath = addNode.DefineLocation.FilePath,
                StartLine = addNode.DefineLocation.StartLine,
                EndLine = addNode.DefineLocation.EndLine
            };
        if (addNode.ImplementationLocation is not null)
            implementationLocation = new()
            {
                FilePath = addNode.ImplementationLocation.FilePath,
                StartLine = addNode.ImplementationLocation.StartLine,
                EndLine = addNode.ImplementationLocation.EndLine
            };

        Node node = new()
        {
            ProjectId = addNode.ProjectId,
            CursorKindSpelling = addNode.CursorKindSpelling,
            Spelling = addNode.Spelling,
            CxType = addNode.CxType,
            DefineLocation = defineLocation,
            ImplementationLocation = implementationLocation
        };

        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Cxx_Nodes.AddAsync(node);
        await dbContext.SaveChangesAsync();

        return node.Id;
    }

    public async Task<Result<Models.CXX.Node>> GetNodeFromDefineLocation(Models.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        DefineLocation? defineLocation = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .ThenInclude(item => item.ImplementationLocation)
            .FirstOrDefaultAsync(item => item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine);

        if (defineLocation is null)
            return Result.Fail<Models.CXX.Node>("Location not found");
        if (defineLocation.Node is null)
            throw new InvalidOperationException("DefineLocation.Node is null");

        Models.CXX.Location domainDefineLocation = new(defineLocation.FilePath, defineLocation.StartLine, defineLocation.EndLine);
        Models.CXX.Location? domainImplementationLocation = null;
        if (defineLocation.Node.ImplementationLocation is not null)
            domainImplementationLocation = new(defineLocation.Node.ImplementationLocation.FilePath, defineLocation.Node.ImplementationLocation.StartLine, defineLocation.Node.ImplementationLocation.EndLine);
        Models.CXX.Node node = new()
        {
            ProjectId = defineLocation.Node.ProjectId,
            Id = defineLocation.Node.Id,
            CursorKindSpelling = defineLocation.Node.CursorKindSpelling,
            Spelling = defineLocation.Node.Spelling,
            CxType = defineLocation.Node.CxType,
            DefineLocation = domainDefineLocation,
            ImplementationLocation = domainImplementationLocation
        };

        return node;
    }

    public async Task<Result> UpdateNodeAsync(Models.CXX.Node node)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node dbNode = new()
        {
            ProjectId = node.ProjectId,
            Id = node.Id,
            CursorKindSpelling = node.CursorKindSpelling,
            Spelling = node.Spelling,
            CxType = node.CxType
        };
        dbContext.Cxx_Nodes.Update(dbNode);

        DefineLocation? defineLocation = await dbContext.Cxx_DefineLocations.FirstOrDefaultAsync(item => item.NodeId == node.Id);
        if (defineLocation is null && node.DefineLocation is not null)
        {
            DefineLocation location = new()
            {
                FilePath = node.DefineLocation.FilePath,
                StartLine = node.DefineLocation.StartLine,
                EndLine = node.DefineLocation.EndLine,
                Node = dbNode
            };
            await dbContext.Cxx_DefineLocations.AddAsync(location);
        }
        else if (defineLocation is not null && node.DefineLocation is not null)
        {
            defineLocation.FilePath = node.DefineLocation!.FilePath;
            defineLocation.StartLine = node.DefineLocation!.StartLine;
            defineLocation.EndLine = node.DefineLocation!.EndLine;
            dbContext.Cxx_DefineLocations.Update(defineLocation);
        }
        else if (defineLocation is not null && node.DefineLocation is null)
        {
            dbContext.Cxx_DefineLocations.Remove(defineLocation);
        }

        ImplementationLocation? implementationLocation = await dbContext.Cxx_ImplementationLocations.FirstOrDefaultAsync(item => item.NodeId == node.Id);
        if (implementationLocation is null && node.ImplementationLocation is not null)
        {
            ImplementationLocation location = new()
            {
                FilePath = node.ImplementationLocation!.FilePath,
                StartLine = node.ImplementationLocation!.StartLine,
                EndLine = node.ImplementationLocation!.EndLine,
                Node = dbNode
            };
            await dbContext.Cxx_ImplementationLocations.AddAsync(location);
        }
        else if (implementationLocation is not null && node.ImplementationLocation is not null)
        {
            implementationLocation.FilePath = node.ImplementationLocation!.FilePath;
            implementationLocation.StartLine = node.ImplementationLocation!.StartLine;
            implementationLocation.EndLine = node.ImplementationLocation!.EndLine;
            dbContext.Cxx_ImplementationLocations.Update(implementationLocation);
        }
        else if (implementationLocation is not null && node.ImplementationLocation is null)
        {
            dbContext.Cxx_ImplementationLocations.Remove(implementationLocation);
        }

        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }
}
