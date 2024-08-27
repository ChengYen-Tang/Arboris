using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.Repositories;

public class CxxRepository(IDbContextFactory<ArborisDbContext> dbContextFactory) : ICxxRepository
{
    public async Task<Guid> AddNodeAsync(Models.Analyze.CXX.AddNode addNode)
    {
        DefineLocation? defineLocation = null;
        ImplementationLocation? implementationLocation = null;

        if (addNode.DefineLocation is not null)
            defineLocation = new()
            {
                FilePath = addNode.DefineLocation.FilePath,
                StartLine = addNode.DefineLocation.StartLine,
                EndLine = addNode.DefineLocation.EndLine,
                SourceCode = addNode.DefineLocation.SourceCode,
                DisplayName = addNode.DefineLocation.CodeDefine
            };
        if (addNode.ImplementationLocation is not null)
            implementationLocation = new()
            {
                FilePath = addNode.ImplementationLocation.FilePath,
                StartLine = addNode.ImplementationLocation.StartLine,
                EndLine = addNode.ImplementationLocation.EndLine,
                SourceCode = addNode.ImplementationLocation.SourceCode,
                DisplayName = addNode.ImplementationLocation.CodeDefine
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

    public async Task<Result<Models.Analyze.CXX.NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync()
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_Nodes.Where(item => item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Select(item => new Models.Analyze.CXX.NodeInfo(item.CursorKindSpelling, item.Spelling, item.CxType, item.NameSpace))
            .Distinct()
            .ToArrayAsync();
    }

    public async Task<Result<ForDescriptionNode>> GetNodeForDescriptionAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes.Include(item => item.DefineLocation).Include(item => item.ImplementationLocation).FirstOrDefaultAsync(item => item.Id == nodeId);
        if (node is null)
            return Result.Fail("Node not found");

        string? sourceCode = node.ImplementationLocation is not null ? node.ImplementationLocation.SourceCode : node.DefineLocation?.SourceCode;
        ForDescriptionNode descriptionNode = new()
        {
            UserDescription = node.UserDescription,
            SourceCode = sourceCode,
        };
        return descriptionNode;
    }

    public async Task<Result<ForUnitTestNode>> GetForUnitTestNodeAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes.Include(item => item.DefineLocation).Include(item => item.ImplementationLocation).FirstOrDefaultAsync(item => item.Id == nodeId);
        if (node is null)
            return Result.Fail("Node not found");

        string? displayName = node.DefineLocation is not null ? node.DefineLocation!.DisplayName : node.ImplementationLocation?.DisplayName;
        ForUnitTestNode unitTestNode = new()
        {
            Description = node.LLMDescription,
            DisplayName = displayName,
            ExampleCode = node.ExampleCode,
        };

        return unitTestNode;
    }

    public async Task<Result<OverViewNode[]>> GetNodeDependenciesAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid[] dependencies = await dbContext.Cxx_NodeDependencies
            .Where(item => item.NodeId == nodeId)
            .Select(item => item.FromId)
            .ToArrayAsync();
        if (dependencies.Length == 0)
            return Array.Empty<OverViewNode>();
        Node[] nodes = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .Where(item => dependencies.Contains(item.Id))
            .ToArrayAsync();

        OverViewNode[] viewNodes = new OverViewNode[nodes.Length];

        for (int i = 0; i < nodes.Length; i++)
        {
            string? displayName = nodes[i].DefineLocation is not null ? nodes[i].DefineLocation!.DisplayName : nodes[i].ImplementationLocation?.DisplayName;
            viewNodes[i] = new OverViewNode
            {
                Description = nodes[i].LLMDescription,
                DisplayName = displayName,
            };
        }
        return viewNodes;
    }

    public async Task<Result<Models.Analyze.CXX.Node>> GetNodeFromDefineLocation(Models.Analyze.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        DefineLocation? defineLocation = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .ThenInclude(item => item!.ImplementationLocation)
            .FirstOrDefaultAsync(item => item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine);

        if (defineLocation is null)
            return Result.Fail<Models.Analyze.CXX.Node>("Location not found");
        if (defineLocation.Node is null)
            throw new InvalidOperationException("DefineLocation.Node is null");

        Models.Analyze.CXX.Location domainDefineLocation = new(defineLocation.FilePath, defineLocation.StartLine, defineLocation.EndLine) { SourceCode = defineLocation.SourceCode, CodeDefine = defineLocation.DisplayName };
        Models.Analyze.CXX.Location? domainImplementationLocation = null;
        if (defineLocation.Node.ImplementationLocation is not null)
            domainImplementationLocation = new(defineLocation.Node.ImplementationLocation.FilePath, defineLocation.Node.ImplementationLocation.StartLine, defineLocation.Node.ImplementationLocation.EndLine) { SourceCode = defineLocation.Node.ImplementationLocation.SourceCode, CodeDefine = defineLocation.Node.ImplementationLocation.DisplayName };
        Models.Analyze.CXX.Node node = new()
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

    public async Task<Result<OverViewNode[]>> GetNodeMembersAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid[] members = await dbContext.Cxx_NodeMembers
            .Where(item => item.NodeId == nodeId)
            .Select(item => item.MemberId)
            .ToArrayAsync();
        if (members.Length == 0)
            return Array.Empty<OverViewNode>();
        Node[] nodes = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .Where(item => members.Contains(item.Id))
            .ToArrayAsync();

        OverViewNode[] viewNodes = new OverViewNode[nodes.Length];

        for (int i = 0; i < nodes.Length; i++)
        {
            string? displayName = nodes[i].DefineLocation is not null ? nodes[i].DefineLocation!.DisplayName : nodes[i].ImplementationLocation?.DisplayName;
            viewNodes[i] = new OverViewNode
            {
                Description = nodes[i].LLMDescription,
                DisplayName = displayName,
            };
        }
        return viewNodes;
    }

    public async Task<Result<OverViewNode[]>> GetNodeTypesAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid[] types = await dbContext.Cxx_NodeTypes
            .Where(item => item.NodeId == nodeId)
            .Select(item => item.TypeId)
            .ToArrayAsync();
        if (types.Length == 0)
            return Array.Empty<OverViewNode>();
        Node[] nodes = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .Where(item => types.Contains(item.Id))
            .ToArrayAsync();

        OverViewNode[] viewNodes = new OverViewNode[nodes.Length];

        for (int i = 0; i < nodes.Length; i++)
        {
            string? displayName = nodes[i].DefineLocation is not null ? nodes[i].DefineLocation!.DisplayName : nodes[i].ImplementationLocation?.DisplayName;
            viewNodes[i] = new OverViewNode
            {
                Description = nodes[i].LLMDescription,
                DisplayName = displayName,
            };
        }
        return viewNodes;
    }

    public async Task<Result<OverallNode[]>> GetOverallNodeAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_Nodes
            .Where(item => item.ProjectId == projectId)
            .Select(item => new OverallNode { Id = item.Id, CursorKindSpelling = item.CursorKindSpelling })
            .ToArrayAsync();
    }

    public async Task<Result<OverallNodeDependency[]>> GetOverallNodeDependencyAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_NodeDependencies
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId)
            .Select(item => new OverallNodeDependency { NodeId = item.NodeId, FromId = item.FromId })
            .ToArrayAsync();
    }

    public async Task<Result<OverallNodeMember[]>> GetOverallNodeMemberAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_NodeMembers
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId)
            .Select(item => new OverallNodeMember { NodeId = item.NodeId, MemberId = item.MemberId })
            .ToArrayAsync();
    }

    public async Task<Result<OverallNodeType[]>> GetOverallNodeTypeAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_NodeTypes
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId)
            .Select(item => new OverallNodeType { NodeId = item.NodeId, TypeId = item.TypeId })
            .ToArrayAsync();
    }

    public async Task<Result> LinkDependencyAsync(Models.Analyze.CXX.Location nodeLocation, Models.Analyze.CXX.Location fromLocation)
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

    public async Task<Result> LinkDependencyCallExprOperatorEqualAsync(Models.Analyze.CXX.Location nodeLocation, Models.Analyze.CXX.Location fromLocation)
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

    public async Task<Result> LinkMemberAsync(Models.Analyze.CXX.Location classLocation, Guid memberId)
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

    public async Task<Result> LinkTypeAsync(Models.Analyze.CXX.Location nodeLocation, Models.Analyze.CXX.Location typeLocation)
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

    public async Task<Result> MoveTypeDeclarationLinkAsync(Models.Analyze.CXX.NodeInfo nodeInfo)
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

        if (haveMembers.Length != 0 && noMembers.Length != 0)
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

    public async Task<Result> UpdateLLMDescriptionAsync(Guid id, string description)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes.FindAsync(id);
        if (node is null)
            return Result.Fail("Node not found");
        node.LLMDescription = description;
        dbContext.Cxx_Nodes.Update(node);
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> UpdateNodeAsync(Models.Analyze.CXX.Node node)
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
                SourceCode = node.DefineLocation.SourceCode,
                DisplayName = node.DefineLocation.CodeDefine,
                Node = dbNode
            };
            await dbContext.Cxx_DefineLocations.AddAsync(location);
        }
        else if (defineLocation is not null && node.DefineLocation is not null)
        {
            defineLocation.FilePath = node.DefineLocation!.FilePath;
            defineLocation.StartLine = node.DefineLocation!.StartLine;
            defineLocation.EndLine = node.DefineLocation!.EndLine;
            defineLocation.SourceCode = node.DefineLocation!.SourceCode;
            defineLocation.DisplayName = node.DefineLocation!.CodeDefine;
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
                SourceCode = node.ImplementationLocation!.SourceCode,
                DisplayName = node.ImplementationLocation!.CodeDefine,
                Node = dbNode
            };
            await dbContext.Cxx_ImplementationLocations.AddAsync(location);
        }
        else if (implementationLocation is not null && node.ImplementationLocation is not null)
        {
            implementationLocation.FilePath = node.ImplementationLocation!.FilePath;
            implementationLocation.StartLine = node.ImplementationLocation!.StartLine;
            implementationLocation.EndLine = node.ImplementationLocation!.EndLine;
            implementationLocation.SourceCode = node.ImplementationLocation!.SourceCode;
            implementationLocation.DisplayName = node.ImplementationLocation!.CodeDefine;
            dbContext.Cxx_ImplementationLocations.Update(implementationLocation);
        }
        else if (implementationLocation is not null && node.ImplementationLocation is null)
        {
            dbContext.Cxx_ImplementationLocations.Remove(implementationLocation);
        }

        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    private async Task<Result<Guid>> GetNodeIdFromLocationAsync(Models.Analyze.CXX.Location location)
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
