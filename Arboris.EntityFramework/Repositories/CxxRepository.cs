using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Arboris.EntityFramework.Repositories;

public class CxxRepository(IDbContextFactory<ArborisDbContext> dbContextFactory) : ICxxRepository
{
    /// <inheritdoc />
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
                DisplayName = addNode.DefineLocation.DisplayName
            };
        if (addNode.ImplementationLocation is not null)
            implementationLocation = new()
            {
                FilePath = addNode.ImplementationLocation.FilePath,
                StartLine = addNode.ImplementationLocation.StartLine,
                EndLine = addNode.ImplementationLocation.EndLine,
                SourceCode = addNode.ImplementationLocation.SourceCode,
                DisplayName = addNode.ImplementationLocation.DisplayName
            };

        Node node = new()
        {
            ProjectId = addNode.ProjectId,
            VcProjectName = addNode.VcProjectName,
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

    /// <inheritdoc />
    public async Task<Result<Models.Analyze.CXX.NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync(Guid projectId, string vcProjectName)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_Nodes.Where(item => item.ProjectId == projectId && item.VcProjectName == vcProjectName && item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Select(item => new Models.Analyze.CXX.NodeInfo(item.Id, item.VcProjectName, item.CursorKindSpelling, item.Spelling, item.CxType, item.NameSpace, item.UserDescription, item.LLMDescription))
            .Distinct()
            .ToArrayAsync();
    }

    /// <inheritdoc />
    public async Task<Result<ForDescriptionNode>> GetNodeForDescriptionAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .FirstOrDefaultAsync(item => item.Id == nodeId);
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

    /// <inheritdoc />
    public async Task<Result<OverViewNode>> GetForUnitTestNodeAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .FirstOrDefaultAsync(item => item.Id == nodeId);
        if (node is null)
            return Result.Fail("Node not found");

        string? displayName = node.DefineLocation is not null ? node.DefineLocation!.DisplayName : node.ImplementationLocation?.DisplayName;
        OverViewNode unitTestNode = new()
        {
            Description = node.LLMDescription,
            DisplayName = displayName
        };

        return unitTestNode;
    }

    /// <inheritdoc />
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

        Parallel.For(0, nodes.Length, i =>
        {
            string? displayName = nodes[i].DefineLocation is not null ? nodes[i].DefineLocation!.DisplayName : nodes[i].ImplementationLocation?.DisplayName;
            viewNodes[i] = new OverViewNode
            {
                Description = nodes[i].LLMDescription,
                DisplayName = displayName,
            };
        });
        return viewNodes;
    }

    /// <inheritdoc />
    public async Task<Result<Models.Analyze.CXX.Node>> GetNodeFromDefineLocationAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        DefineLocation? defineLocation = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .ThenInclude(item => item!.ImplementationLocation)
            .FirstOrDefaultAsync(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine);

        if (defineLocation is null)
            return Result.Fail<Models.Analyze.CXX.Node>($"Location not found. Start line: {location.StartLine}, End Line: {location.EndLine}, File: {location.FilePath}");
        if (defineLocation.Node is null)
            throw new InvalidOperationException("DefineLocation.Node is null");

        Models.Analyze.CXX.Location domainDefineLocation = new(defineLocation.FilePath, defineLocation.StartLine, defineLocation.EndLine) { SourceCode = defineLocation.SourceCode, DisplayName = defineLocation.DisplayName };
        Models.Analyze.CXX.Location? domainImplementationLocation = null;
        if (defineLocation.Node.ImplementationLocation is not null)
            domainImplementationLocation = new(defineLocation.Node.ImplementationLocation.FilePath, defineLocation.Node.ImplementationLocation.StartLine, defineLocation.Node.ImplementationLocation.EndLine) { SourceCode = defineLocation.Node.ImplementationLocation.SourceCode, DisplayName = defineLocation.Node.ImplementationLocation.DisplayName };
        Models.Analyze.CXX.Node node = new()
        {
            ProjectId = defineLocation.Node.ProjectId,
            VcProjectName = defineLocation.Node.VcProjectName,
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

    /// <inheritdoc />
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

        Parallel.For(0, nodes.Length, i =>
        {
            string? displayName = nodes[i].DefineLocation is not null ? nodes[i].DefineLocation!.DisplayName : nodes[i].ImplementationLocation?.DisplayName;
            viewNodes[i] = new OverViewNode
            {
                Description = nodes[i].LLMDescription,
                DisplayName = displayName,
            };
        });
        return viewNodes;
    }

    /// <inheritdoc />
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

        Parallel.For(0, nodes.Length, i =>
        {
            string? displayName = nodes[i].DefineLocation is not null ? nodes[i].DefineLocation!.DisplayName : nodes[i].ImplementationLocation?.DisplayName;
            viewNodes[i] = new OverViewNode
            {
                Description = nodes[i].LLMDescription,
                DisplayName = displayName,
            };
        });
        return viewNodes;
    }

    /// <inheritdoc />
    public async Task<Result<OverallNode[]>> GetOverallNodeAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_Nodes
            .Where(item => item.ProjectId == projectId)
            .Select(item => new OverallNode { Id = item.Id, CursorKindSpelling = item.CursorKindSpelling })
            .ToArrayAsync();
    }

    /// <inheritdoc />
    public async Task<Result<OverallNodeDependency[]>> GetOverallNodeDependencyAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_NodeDependencies
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId)
            .Select(item => new OverallNodeDependency { NodeId = item.NodeId, FromId = item.FromId })
            .ToArrayAsync();
    }

    /// <inheritdoc />
    public async Task<Result<OverallNodeMember[]>> GetOverallNodeMemberAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_NodeMembers
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId)
            .Select(item => new OverallNodeMember { NodeId = item.NodeId, MemberId = item.MemberId })
            .ToArrayAsync();
    }

    /// <inheritdoc />
    public async Task<Result<OverallNodeType[]>> GetOverallNodeTypeAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_NodeTypes
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId)
            .Select(item => new OverallNodeType { NodeId = item.NodeId, TypeId = item.TypeId })
            .ToArrayAsync();
    }

    /// <inheritdoc />
    public async Task<Result> LinkDependencyAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location nodeLocation, Models.Analyze.CXX.Location fromLocation)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Result<Guid> NodeId = await GetNodeIdFromLocationAsync(projectId, vcProjectName, nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail($"Node location not found. Start line: {nodeLocation.StartLine}, End Line: {nodeLocation.EndLine}, File: {nodeLocation.FilePath}");

        Result<Guid> FromId = await GetNodeIdFromLocationAsync(projectId, vcProjectName, fromLocation);
        if (FromId.IsFailed)
            return Result.Fail($"From location not found. Start line: {fromLocation.StartLine}, End Line: {fromLocation.EndLine}, File: {fromLocation.FilePath}");

        if (await dbContext.Cxx_NodeDependencies.AnyAsync(item => item.NodeId == NodeId.Value && item.FromId == FromId.Value))
            return Result.Ok();

        await dbContext.Cxx_NodeDependencies.AddAsync(new() { NodeId = NodeId.Value, FromId = FromId.Value });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> LinkDependencyCallExprOperatorEqualAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location nodeLocation, Models.Analyze.CXX.Location fromLocation)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Result<Guid> NodeId = await GetNodeIdFromLocationAsync(projectId, vcProjectName, nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail($"Node location not found. Start line: {nodeLocation.StartLine}, End Line: {nodeLocation.EndLine}, File: {nodeLocation.FilePath}");

        Guid FromId = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"))
            .Select(item => item.NodeId)
            .FirstOrDefaultAsync();
        if (FromId == Guid.Empty)
            FromId = await dbContext.Cxx_ImplementationLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"))
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        if (FromId == Guid.Empty)
            return Result.Fail($"From location not found. Start line: {fromLocation.StartLine}, End Line: {fromLocation.EndLine}, File: {fromLocation.FilePath}");

        if (await dbContext.Cxx_NodeDependencies.AnyAsync(item => item.NodeId == NodeId.Value && item.FromId == FromId))
            return Result.Ok();

        await dbContext.Cxx_NodeDependencies.AddAsync(new() { NodeId = NodeId.Value, FromId = FromId });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> LinkMemberAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location classLocation, Guid memberId)
    {
        Result<Guid> NodeId = await GetNodeIdFromLocationAsync(projectId, vcProjectName, classLocation);
        if (NodeId.IsFailed)
            return Result.Fail($"Node location not found. Start line: {classLocation.StartLine}, End Line: {classLocation.EndLine}, File: {classLocation.FilePath}");
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        if (!await dbContext.Cxx_Nodes.AnyAsync(item => item.Id == memberId && item.ProjectId == projectId && item.VcProjectName == vcProjectName))
            return Result.Fail($"Member not found. Member id: {memberId}");
        await dbContext.Cxx_NodeMembers.AddAsync(new() { NodeId = NodeId.Value, MemberId = memberId });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> LinkTypeAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location nodeLocation, Models.Analyze.CXX.Location typeLocation)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Result<Guid> NodeId = await GetNodeIdFromLocationAsync(projectId, vcProjectName, nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail($"Node location not found. Start line: {nodeLocation.StartLine}, End Line: {nodeLocation.EndLine}, File: {nodeLocation.FilePath}");

        Result<Guid> TypeId = await GetNodeIdFromLocationAsync(projectId, vcProjectName, typeLocation);
        if (TypeId.IsFailed)
            return Result.Fail($"Type location not found. Start line: {typeLocation.StartLine}, End Line: {typeLocation.EndLine}, File: {typeLocation.FilePath}");

        if (await dbContext.Cxx_NodeTypes.AnyAsync(item => item.NodeId == NodeId.Value && item.TypeId == TypeId.Value))
            return Result.Ok();

        await dbContext.Cxx_NodeTypes.AddAsync(new() { NodeId = NodeId.Value, TypeId = TypeId.Value });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result> MoveTypeDeclarationLinkAsync(Guid projectId, Models.Analyze.CXX.NodeInfo nodeInfo)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid[] haveMembers = await dbContext.Cxx_Nodes.Include(item => item.Members)
            .Where(item => item.ProjectId == projectId && item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Where(item => item.VcProjectName == nodeInfo.VcProjectName && item.Spelling == nodeInfo.Spelling && item.CxType == nodeInfo.CxType && item.NameSpace == nodeInfo.NameSpace)
            .Where(item => item.Members.Count > 0)
            .Select(item => item.Id)
            .ToArrayAsync();
        Guid[] noMembers = await dbContext.Cxx_Nodes.Include(item => item.Members)
            .Where(item => item.ProjectId == projectId && item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Where(item => item.VcProjectName == nodeInfo.VcProjectName && item.Spelling == nodeInfo.Spelling && item.CxType == nodeInfo.CxType && item.NameSpace == nodeInfo.NameSpace)
            .Where(item => item.Members.Count == 0)
            .Select(item => item.Id)
            .ToArrayAsync();

        if (haveMembers.Length != 0 && noMembers.Length != 0)
        {
            NodeType[] removeTypes = await dbContext.Cxx_NodeTypes
                .Where(item => noMembers.Contains(item.TypeId))
                .ToArrayAsync();
            foreach (NodeType addType in removeTypes.Select(item => new NodeType { NodeId = item.NodeId, TypeId = haveMembers[0] }))
            {
                if (!await dbContext.Cxx_NodeTypes.AnyAsync(item => item.NodeId == addType.NodeId && item.TypeId == addType.TypeId))
                    await dbContext.Cxx_NodeTypes.AddAsync(addType);
            }
            if (removeTypes.Length > 0)
            {
                dbContext.RemoveRange(removeTypes);
                await dbContext.SaveChangesAsync();
            }
        }
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveTypeDeclarations(Guid projectId, string vcProjectName)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid[] nodesId = await dbContext.Cxx_Nodes.Where(item => item.ProjectId == projectId && item.VcProjectName == vcProjectName && item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Select(item => item.Id)
            .ToArrayAsync();

        List<Guid> removeId = new(nodesId);
        foreach (Guid nodeId in nodesId)
        {
            if (await dbContext.Cxx_NodeMembers.AnyAsync(item => item.NodeId == nodeId || item.MemberId == nodeId))
                removeId.Remove(nodeId);
            else if (await dbContext.Cxx_NodeDependencies.AnyAsync(item => item.FromId == nodeId || item.NodeId == nodeId))
                removeId.Remove(nodeId);
            else if (await dbContext.Cxx_NodeTypes.AnyAsync(item => item.TypeId == nodeId || item.NodeId == nodeId))
                removeId.Remove(nodeId);
        }
        dbContext.Cxx_Nodes.RemoveRange(removeId.Select(id => new Node { Id = id }));
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public async Task<Result> UpdateNodeLocationAsync(Models.Analyze.CXX.NodeWithLocationDto node)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? dbNode = await dbContext.Cxx_Nodes.FindAsync(node.NodeId);
        if (dbNode is null)
            return Result.Fail("Node not found");

        DefineLocation? defineLocation = await dbContext.Cxx_DefineLocations.FirstOrDefaultAsync(item => item.NodeId == node.NodeId);
        if (defineLocation is null && node.DefineLocation is not null)
        {
            DefineLocation location = new()
            {
                FilePath = node.DefineLocation.FilePath,
                StartLine = node.DefineLocation.StartLine,
                EndLine = node.DefineLocation.EndLine,
                SourceCode = node.DefineLocation.SourceCode,
                DisplayName = node.DefineLocation.DisplayName,
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
            defineLocation.DisplayName = node.DefineLocation!.DisplayName;
            dbContext.Cxx_DefineLocations.Update(defineLocation);
        }
        else if (defineLocation is not null && node.DefineLocation is null)
        {
            dbContext.Cxx_DefineLocations.Remove(defineLocation);
        }

        ImplementationLocation? implementationLocation = await dbContext.Cxx_ImplementationLocations.FirstOrDefaultAsync(item => item.NodeId == node.NodeId);
        if (implementationLocation is null && node.ImplementationLocation is not null)
        {
            ImplementationLocation location = new()
            {
                FilePath = node.ImplementationLocation!.FilePath,
                StartLine = node.ImplementationLocation!.StartLine,
                EndLine = node.ImplementationLocation!.EndLine,
                SourceCode = node.ImplementationLocation!.SourceCode,
                DisplayName = node.ImplementationLocation!.DisplayName,
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
            implementationLocation.DisplayName = node.ImplementationLocation!.DisplayName;
            dbContext.Cxx_ImplementationLocations.Update(implementationLocation);
        }
        else if (implementationLocation is not null && node.ImplementationLocation is null)
        {
            dbContext.Cxx_ImplementationLocations.Remove(implementationLocation);
        }

        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    private async Task<Result<Guid>> GetNodeIdFromLocationAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid NodeId = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine)
            .Select(item => item.NodeId)
            .FirstOrDefaultAsync();
        if (NodeId == Guid.Empty)
            NodeId = await dbContext.Cxx_ImplementationLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine)
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        if (NodeId == Guid.Empty)
            return Result.Fail("Node location not found");
        return NodeId;
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CheckNodeExistsAsync(Models.Analyze.CXX.AddNode addNode)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .FirstOrDefaultAsync(item => item.ProjectId == addNode.ProjectId && item.VcProjectName == addNode.VcProjectName && item.CursorKindSpelling == addNode.CursorKindSpelling && item.Spelling == addNode.Spelling && item.CxType == addNode.CxType && item.NameSpace == addNode.NameSpace && item.DefineLocation!.FilePath == addNode.DefineLocation!.FilePath && item.DefineLocation.StartLine == addNode.DefineLocation.StartLine && item.DefineLocation.EndLine == addNode.DefineLocation.EndLine);

        if (node is null)
            return Result.Fail<Guid>("Node not found");

        return node.Id;
    }

    /// <inheritdoc />
    public async Task<Models.Analyze.CXX.NodeInfoWithLocation[]> GetNodesFromProjectAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        var nodes = await dbContext.Cxx_Nodes
            .Where(item => item.ProjectId == projectId)
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .Select(item => new { item.Id, item.VcProjectName, item.CursorKindSpelling, item.Spelling, item.CxType, item.NameSpace, item.UserDescription, item.LLMDescription, item.DefineLocation, item.ImplementationLocation })
            .ToArrayAsync();

        return nodes.AsParallel().Select(item =>
        {
            Models.Analyze.CXX.Location? defineLocation = null;
            if (item.DefineLocation is not null)
                defineLocation = new Models.Analyze.CXX.Location(item.DefineLocation.FilePath, item.DefineLocation.StartLine, item.DefineLocation.EndLine) { SourceCode = item.DefineLocation.SourceCode, DisplayName = item.DefineLocation.DisplayName };
            Models.Analyze.CXX.Location? implementationLocation = null;
            if (item.ImplementationLocation is not null)
                implementationLocation = new Models.Analyze.CXX.Location(item.ImplementationLocation.FilePath, item.ImplementationLocation.StartLine, item.ImplementationLocation.EndLine) { SourceCode = item.ImplementationLocation.SourceCode, DisplayName = item.ImplementationLocation.DisplayName };
            return new Models.Analyze.CXX.NodeInfoWithLocation(item.Id, item.VcProjectName, item.CursorKindSpelling, item.Spelling, item.CxType, item.NameSpace, item.UserDescription, item.LLMDescription, defineLocation, implementationLocation);
        }).ToArray();
    }

    /// <inheritdoc />
    public async Task<string?> GetClassFromNodeAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_NodeMembers
            .Include(item => item.Node)
            .Where(item => item.MemberId == nodeId)
            .Select(item => item.Node.Spelling)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public async Task<Result<Models.Analyze.CXX.Node>> GetNodeAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .FirstOrDefaultAsync(item => item.Id == nodeId);
        if (node is null)
            return Result.Fail<Models.Analyze.CXX.Node>("Node not found");

        Models.Analyze.CXX.Location? defineLocation = null;
        if (node.DefineLocation is not null)
            defineLocation = new Models.Analyze.CXX.Location(node.DefineLocation.FilePath, node.DefineLocation.StartLine, node.DefineLocation.EndLine) { SourceCode = node.DefineLocation.SourceCode, DisplayName = node.DefineLocation.DisplayName };
        Models.Analyze.CXX.Location? implementationLocation = null;
        if (node.ImplementationLocation is not null)
            implementationLocation = new Models.Analyze.CXX.Location(node.ImplementationLocation.FilePath, node.ImplementationLocation.StartLine, node.ImplementationLocation.EndLine) { SourceCode = node.ImplementationLocation.SourceCode, DisplayName = node.ImplementationLocation.DisplayName };
        return new Models.Analyze.CXX.Node()
        {
            ProjectId = node.ProjectId,
            VcProjectName = node.VcProjectName,
            Id = node.Id,
            CursorKindSpelling = node.CursorKindSpelling,
            Spelling = node.Spelling,
            CxType = node.CxType,
            NameSpace = node.NameSpace,
            DefineLocation = defineLocation,
            ImplementationLocation = implementationLocation
        };
    }

    /// <inheritdoc />
    public async Task<Result> UpdateUserDescriptionAsync(Guid projectId, string vcProjectName, string? nameSpace, string? className, string? spelling, string? cxType, string? description)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = string.IsNullOrEmpty(className) ?
            await dbContext.Cxx_Nodes.FirstOrDefaultAsync(item => item.ProjectId == projectId && item.VcProjectName == vcProjectName && item.NameSpace == nameSpace && item.Spelling == spelling && item.CxType == cxType) :
            await dbContext.Cxx_NodeMembers
            .Include(item => item.Node)
            .Include(item => item.Member)
            .Where(item => item.Member.ProjectId == projectId && item.Member.VcProjectName == vcProjectName && item.Member.NameSpace == nameSpace && item.Member.Spelling == spelling && item.Member.CxType == cxType && item.Node.Spelling == className)
            .Select(item => item.Member).FirstOrDefaultAsync();

        if (node is null)
            return Result.Fail("Node not found");

        node.UserDescription = description;
        dbContext.Cxx_Nodes.Update(node);
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }
}
