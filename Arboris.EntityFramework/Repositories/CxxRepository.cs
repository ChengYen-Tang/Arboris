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
            NameSpace = addNode.NameSpace,
            DefineLocation = defineLocation,
            ImplementationLocation = implementationLocation
        };

        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Cxx_Nodes.AddAsync(node);
        await dbContext.SaveChangesAsync();

        return node.Id;
    }

    public async Task<Result<Models.CXX.NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync()
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_Nodes.Where(item => item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Select(item => new Models.CXX.NodeInfo(item.CursorKindSpelling, item.Spelling, item.CxType, item.NameSpace))
            .Distinct()
            .ToArrayAsync();
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
            NameSpace = defineLocation.Node.NameSpace,
            DefineLocation = domainDefineLocation,
            ImplementationLocation = domainImplementationLocation
        };

        return node;
    }

    public async Task<Result> LinkDependencyAsync(Models.CXX.Location nodeLocation, Models.CXX.Location fromLocation)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Result<Guid> NodeId = await GetNodeIdFromLocationAsync(nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail("Node location not found");

        Result<Guid> FromId = await GetNodeIdFromLocationAsync(fromLocation);
        if (FromId.IsFailed)
            return Result.Fail("From location not found");

        if (await dbContext.Cxx_NodeDependencies.AnyAsync(item => item.NodeId == NodeId.Value && item.FromId == FromId.Value))
            return Result.Ok();

        await dbContext.Cxx_NodeDependencies.AddAsync(new() { NodeId = NodeId.Value, FromId = FromId.Value });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> LinkDependencyCallExprOperatorEqualAsync(Models.CXX.Location nodeLocation, Models.CXX.Location fromLocation)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Result<Guid> NodeId = await GetNodeIdFromLocationAsync(nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail("Node location not found");

        Guid FromId = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .Where(item => item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"))
            .Select(item => item.NodeId)
            .FirstOrDefaultAsync();
        if (FromId == Guid.Empty)
            FromId = await dbContext.Cxx_ImplementationLocations
                .Include(item => item.Node)
                .Where(item => item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"))
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        if (FromId == Guid.Empty)
            return Result.Fail("From location not found");

        if (await dbContext.Cxx_NodeDependencies.AnyAsync(item => item.NodeId == NodeId.Value && item.FromId == FromId))
            return Result.Ok();

        await dbContext.Cxx_NodeDependencies.AddAsync(new() { NodeId = NodeId.Value, FromId = FromId });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> LinkMemberAsync(Models.CXX.Location classLocation, Guid memberId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        DefineLocation? defineLocation = await dbContext.Cxx_DefineLocations
            .FirstOrDefaultAsync(item => item.FilePath == classLocation.FilePath && item.StartLine == classLocation.StartLine && item.EndLine == classLocation.EndLine);
        if (defineLocation is null)
            return Result.Fail("Class location not found");
        await dbContext.Cxx_NodeMembers.AddAsync(new() { NodeId = defineLocation.NodeId, MemberId = memberId });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> LinkTypeAsync(Models.CXX.Location nodeLocation, Models.CXX.Location typeLocation)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Result<Guid> NodeId = await GetNodeIdFromLocationAsync(nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail("Node location not found");

        Result<Guid> TypeId = await GetNodeIdFromLocationAsync(typeLocation);
        if (TypeId.IsFailed)
            return Result.Fail("Type location not found");

        if (await dbContext.Cxx_NodeTypes.AnyAsync(item => item.NodeId == NodeId.Value && item.TypeId == TypeId.Value))
            return Result.Ok();

        await dbContext.Cxx_NodeTypes.AddAsync(new() { NodeId = NodeId.Value, TypeId = TypeId.Value });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> MoveTypeDeclarationTypeAsync(Models.CXX.NodeInfo nodeInfo)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid[] haveMembers = await dbContext.Cxx_Nodes.Include(item => item.Members)
            .Where(item => item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Where(item => item.Spelling == nodeInfo.Spelling && item.CxType == nodeInfo.CxType && item.NameSpace == nodeInfo.NameSpace)
            .Where(item => item.Members.Count > 0)
            .Select(item => item.Id)
            .ToArrayAsync();
        Guid[] noMembers = await dbContext.Cxx_Nodes.Include(item => item.Members)
            .Where(item => item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Where(item => item.Spelling == nodeInfo.Spelling && item.CxType == nodeInfo.CxType && item.NameSpace == nodeInfo.NameSpace)
            .Where(item => item.Members.Count == 0)
            .Select(item => item.Id)
            .ToArrayAsync();

        if (haveMembers.Any() && noMembers.Any())
        {
            NodeType[] removeTypes = await dbContext.Cxx_NodeTypes
                .Where(item => noMembers.Contains(item.TypeId))
                .ToArrayAsync();
            NodeType[] addTypes = removeTypes.Select(item => new NodeType { NodeId = item.NodeId, TypeId = haveMembers[0] }).ToArray();
            await dbContext.AddRangeAsync(addTypes);
            dbContext.RemoveRange(removeTypes);
            await dbContext.SaveChangesAsync();
        }
        return Result.Ok();
    }

    public async Task<Result> RemoveTypeDeclarations()
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid[] nodesId = await dbContext.Cxx_Nodes.Where(item => item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Select(item => item.Id)
            .ToArrayAsync();

        List<Guid> removeId = new(nodesId);
        foreach (Guid nodeId in nodesId)
        {
            if (await dbContext.Cxx_NodeMembers.AnyAsync(item => item.NodeId == nodeId))
                removeId.Remove(nodeId);
            else if (await dbContext.Cxx_NodeDependencies.AnyAsync(item => item.FromId == nodeId))
                removeId.Remove(nodeId);
            else if (await dbContext.Cxx_NodeTypes.AnyAsync(item => item.TypeId == nodeId))
                removeId.Remove(nodeId);
        }
        dbContext.Cxx_Nodes.RemoveRange(removeId.Select(id => new Node { Id = id }));
        await dbContext.SaveChangesAsync();
        return Result.Ok();
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
            CxType = node.CxType,
            NameSpace = node.NameSpace
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

    private async Task<Result<Guid>> GetNodeIdFromLocationAsync(Models.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid NodeId = await dbContext.Cxx_DefineLocations
            .Where(item => item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine)
            .Select(item => item.NodeId)
            .FirstOrDefaultAsync();
        if (NodeId == Guid.Empty)
            NodeId = await dbContext.Cxx_ImplementationLocations
                .Where(item => item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine)
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        if (NodeId == Guid.Empty)
            return Result.Fail("Node location not found");
        return NodeId;
    }
}
