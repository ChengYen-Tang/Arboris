using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Models.Graph.CXX;
using Arboris.Repositories;
using FluentResults;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace Arboris.EntityFramework.Repositories;

public class CxxRepository(IDbContextFactory<ArborisDbContext> dbContextFactory) : ICxxRepository
{
    /// <inheritdoc />
    public async Task<Guid> AddNodeAsync(Models.Analyze.CXX.AddNode addNode)
    {
        DefineLocation? defineLocation = null;
        ImplementationLocation[]? implementationLocation = null;

        if (addNode.DefineLocation is not null)
            defineLocation = new()
            {
                FilePath = addNode.DefineLocation.FilePath,
                StartLine = addNode.DefineLocation.StartLine,
                StartColumn = addNode.DefineLocation.StartColumn,
                EndLine = addNode.DefineLocation.EndLine,
                EndColumn = addNode.DefineLocation.EndColumn,
                SourceCode = addNode.DefineLocation.SourceCode?.Value,
                DisplayName = addNode.DefineLocation.DisplayName?.Value
            };
        if (addNode.ImplementationLocation is not null)
            implementationLocation = [new()
            {
                FilePath = addNode.ImplementationLocation.FilePath,
                StartLine = addNode.ImplementationLocation.StartLine,
                StartColumn = addNode.ImplementationLocation.StartColumn,
                EndLine = addNode.ImplementationLocation.EndLine,
                EndColumn = addNode.ImplementationLocation.EndColumn,
                SourceCode = addNode.ImplementationLocation.SourceCode?.Value,
                DisplayName = addNode.ImplementationLocation.DisplayName?.Value
            }];

        Node node = new()
        {
            ProjectId = addNode.ProjectId,
            VcProjectName = addNode.VcProjectName,
            CursorKindSpelling = addNode.CursorKindSpelling,
            Spelling = addNode.Spelling,
            CxType = addNode.CxType,
            NameSpace = addNode.NameSpace,
            DefineLocation = defineLocation,
            ImplementationsLocation = implementationLocation,
            AccessSpecifiers = addNode.AccessSpecifiers
        };

        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Cxx_Nodes.AddAsync(node);
        await dbContext.SaveChangesAsync();

        return node.Id;
    }

    /// <inheritdoc />
    public async Task<bool> CheckImplementationNodeExistsAsync(Models.Analyze.CXX.AddNode addNode)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_ImplementationLocations.Include(item => item.Node).ThenInclude(item => item!.DefineLocation)
            .AnyAsync(item => item.Node!.ProjectId == addNode.ProjectId &&
            item.Node!.VcProjectName == addNode.VcProjectName &&
            item.Node!.CursorKindSpelling == addNode.CursorKindSpelling &&
            item.Node!.Spelling == addNode.Spelling &&
            item.Node!.CxType == addNode.CxType &&
            item.Node!.NameSpace == addNode.NameSpace &&
            item.Node!.DefineLocation == null &&
            item.FilePath == addNode.ImplementationLocation!.FilePath &&
            item.StartLine == addNode.ImplementationLocation!.StartLine &&
            item.EndLine == addNode.ImplementationLocation!.EndLine &&
            item.StartColumn == addNode.ImplementationLocation!.StartColumn &&
            item.EndColumn == addNode.ImplementationLocation!.EndColumn);
    }

    /// <inheritdoc />
    public async Task<Result<Guid>> CheckDefineNodeExistsAsync(Models.Analyze.CXX.AddNode addNode)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .FirstOrDefaultAsync(item => item.ProjectId == addNode.ProjectId && item.VcProjectName == addNode.VcProjectName && item.CursorKindSpelling == addNode.CursorKindSpelling && item.Spelling == addNode.Spelling && item.CxType == addNode.CxType && item.NameSpace == addNode.NameSpace && item.DefineLocation!.FilePath == addNode.DefineLocation!.FilePath && item.DefineLocation.StartLine == addNode.DefineLocation.StartLine && item.DefineLocation.EndLine == addNode.DefineLocation.EndLine && item.DefineLocation.StartColumn == addNode.DefineLocation.StartColumn && item.DefineLocation.EndColumn == addNode.DefineLocation.EndColumn);

        if (node is null)
            return Result.Fail<Guid>("Node not found");

        return node.Id;
    }

    /// <inheritdoc />
    public async Task<Result<Models.Analyze.CXX.NodeInfo[]>> GetDistinctClassAndStructNodeInfosAsync(Guid projectId, string vcProjectName)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        return await dbContext.Cxx_Nodes.Where(item => item.ProjectId == projectId && item.VcProjectName == vcProjectName && item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")
            .Select(item => new Models.Analyze.CXX.NodeInfo(item.Id, item.VcProjectName, item.CursorKindSpelling, item.Spelling, item.CxType, item.AccessSpecifiers, item.NameSpace, item.UserDescription, item.LLMDescription))
            .Distinct()
            .ToArrayAsync();
    }

    /// <inheritdoc />
    public async Task<Result<Models.Analyze.CXX.Node>> GetNodeFromDefineLocationAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        DefineLocation? defineLocation = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .ThenInclude(item => item!.ImplementationsLocation)
            .FirstOrDefaultAsync(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn);

        if (defineLocation is null)
            return Result.Fail<Models.Analyze.CXX.Node>($"Location not found. Start line: {location.StartLine}, End Line: {location.EndLine}, File: {location.FilePath}");
        if (defineLocation.Node is null)
            throw new InvalidOperationException("DefineLocation.Node is null");

        Models.Analyze.CXX.Location domainDefineLocation = new(defineLocation.FilePath, defineLocation.StartLine, defineLocation.StartColumn, defineLocation.EndLine, defineLocation.EndColumn) { SourceCode = new(defineLocation.SourceCode), DisplayName = new(defineLocation.DisplayName) };
        ICollection<Models.Analyze.CXX.Location> domainImplementationLocation = [.. defineLocation.Node.ImplementationsLocation.AsParallel().Select(item => new Models.Analyze.CXX.Location(item.FilePath, item.StartLine, item.StartColumn, item.EndLine, item.EndColumn)
        {
            SourceCode = new(item.SourceCode),
            DisplayName = new(item.DisplayName)
        })];
        Models.Analyze.CXX.Node node = new()
        {
            ProjectId = defineLocation.Node.ProjectId,
            VcProjectName = defineLocation.Node.VcProjectName,
            Id = defineLocation.Node.Id,
            CursorKindSpelling = defineLocation.Node.CursorKindSpelling,
            Spelling = defineLocation.Node.Spelling,
            CxType = defineLocation.Node.CxType,
            AccessSpecifiers = defineLocation.Node.AccessSpecifiers,
            NameSpace = defineLocation.Node.NameSpace,
            DefineLocation = domainDefineLocation,
            ImplementationsLocation = domainImplementationLocation,
        };

        return node;
    }

    /// <inheritdoc />
    public async Task<Result<Models.Analyze.CXX.Node>> GetNodeFromDefineLocationCanCrossProjectAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        int locationCount = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .ThenInclude(item => item!.ImplementationsLocation)
            .CountAsync(item => item.Node!.ProjectId == projectId && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn);

        if (locationCount == 0)
            return Result.Fail<Models.Analyze.CXX.Node>($"Location not found. Start line: {location.StartLine}, End Line: {location.EndLine}, File: {location.FilePath}");

        DefineLocation? defineLocation = null;
        if (locationCount > 1)
        {
            defineLocation = await dbContext.Cxx_DefineLocations
                .Include(item => item.Node)
                .ThenInclude(item => item!.ImplementationsLocation)
                .FirstOrDefaultAsync(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn);
        }
        defineLocation ??= await dbContext.Cxx_DefineLocations
                .Include(item => item.Node)
                .ThenInclude(item => item!.ImplementationsLocation)
                .FirstOrDefaultAsync(item => item.Node!.ProjectId == projectId && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn);

        if (defineLocation!.Node is null)
            throw new InvalidOperationException("DefineLocation.Node is null");

        Models.Analyze.CXX.Location domainDefineLocation = new(defineLocation.FilePath, defineLocation.StartLine, defineLocation.StartColumn, defineLocation.EndLine, defineLocation.EndColumn) { SourceCode = new(defineLocation.SourceCode), DisplayName = new(defineLocation.DisplayName) };
        ICollection<Models.Analyze.CXX.Location> domainImplementationLocation = [.. defineLocation.Node.ImplementationsLocation.AsParallel().Select(item => new Models.Analyze.CXX.Location(item.FilePath, item.StartLine, item.StartColumn, item.EndLine, item.EndColumn)
        {
            SourceCode = new(item.SourceCode),
            DisplayName = new(item.DisplayName)
        })];
        Models.Analyze.CXX.Node node = new()
        {
            ProjectId = defineLocation.Node.ProjectId,
            VcProjectName = defineLocation.Node.VcProjectName,
            Id = defineLocation.Node.Id,
            CursorKindSpelling = defineLocation.Node.CursorKindSpelling,
            Spelling = defineLocation.Node.Spelling,
            CxType = defineLocation.Node.CxType,
            AccessSpecifiers = defineLocation.Node.AccessSpecifiers,
            NameSpace = defineLocation.Node.NameSpace,
            DefineLocation = domainDefineLocation,
            ImplementationsLocation = domainImplementationLocation
        };

        return node;
    }

    private async Task<Result<Guid>> GetNodeIdFromLocationAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Guid NodeId = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn)
            .Select(item => item.NodeId)
            .FirstOrDefaultAsync();
        if (NodeId == Guid.Empty)
            NodeId = await dbContext.Cxx_ImplementationLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn)
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        if (NodeId == Guid.Empty)
            return Result.Fail("Node location not found");
        return NodeId;
    }

    private async Task<Result<Guid>> GetNodeIdFromLocationCanCrossProjectAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location location)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();

        int locationCount = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .CountAsync(item => item.Node!.ProjectId == projectId && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn);

        Guid nodeId = Guid.Empty;
        if (locationCount > 1)
        {
            nodeId = await dbContext.Cxx_DefineLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn)
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        }
        if (nodeId == Guid.Empty && locationCount > 0)
        {
            nodeId = await dbContext.Cxx_DefineLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn)
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        }

        if (nodeId != Guid.Empty)
            return nodeId;

        locationCount = await dbContext.Cxx_ImplementationLocations
            .Include(item => item.Node)
            .CountAsync(item => item.Node!.ProjectId == projectId && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn);

        if (locationCount > 1)
        {
            nodeId = await dbContext.Cxx_ImplementationLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn)
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        }
        if (nodeId == Guid.Empty && locationCount > 0)
        {
            nodeId = await dbContext.Cxx_ImplementationLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.FilePath == location.FilePath && item.StartLine == location.StartLine && item.EndLine == location.EndLine && item.StartColumn == location.StartColumn && item.EndColumn == location.EndColumn)
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        }

        if (nodeId != Guid.Empty)
            return nodeId;

        return Result.Fail("Node location not found");
    }


    /// <inheritdoc />
    public async Task<Models.Analyze.CXX.NodeInfoWithLocation[]> GetNodesFromProjectAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node[] nodes = await dbContext.Cxx_Nodes
            .Where(item => item.ProjectId == projectId)
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationsLocation)
            .ToArrayAsync();

        return nodes.AsParallel().Select(item =>
        {
            Models.Analyze.CXX.Location? defineLocation = null;
            if (item.DefineLocation is not null)
                defineLocation = new Models.Analyze.CXX.Location(item.DefineLocation.FilePath, item.DefineLocation.StartLine, item.DefineLocation.StartColumn, item.DefineLocation.EndLine, item.DefineLocation.EndColumn) { SourceCode = new(item.DefineLocation.SourceCode), DisplayName = new(item.DefineLocation.DisplayName) };
            ICollection<Models.Analyze.CXX.Location> implementationLocation = [.. item.ImplementationsLocation.AsParallel().Select(item1 => new Models.Analyze.CXX.Location(item1.FilePath, item1.StartLine, item1.StartColumn, item1.EndLine, item1.EndColumn)
            {
                SourceCode = new(item1.SourceCode),
                DisplayName = new(item1.DisplayName)
            })];
            return new Models.Analyze.CXX.NodeInfoWithLocation(item.Id, item.VcProjectName, item.CursorKindSpelling, item.Spelling, item.CxType, item.AccessSpecifiers, item.NameSpace, item.UserDescription, item.LLMDescription, defineLocation, implementationLocation);
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
            .Include(item => item.ImplementationsLocation)
            .FirstOrDefaultAsync(item => item.Id == nodeId);
        if (node is null)
            return Result.Fail<Models.Analyze.CXX.Node>("Node not found");

        Models.Analyze.CXX.Location? defineLocation = null;
        if (node.DefineLocation is not null)
            defineLocation = new Models.Analyze.CXX.Location(node.DefineLocation.FilePath, node.DefineLocation.StartLine, node.DefineLocation.StartColumn, node.DefineLocation.EndLine, node.DefineLocation.EndColumn) { SourceCode = new(node.DefineLocation.SourceCode), DisplayName = new(node.DefineLocation.DisplayName) };
        ICollection<Models.Analyze.CXX.Location> implementationLocation = [.. node.ImplementationsLocation.AsParallel().Select(item => new Models.Analyze.CXX.Location(item.FilePath, item.StartLine, item.StartColumn, item.EndLine, item.EndColumn)
        {
            SourceCode = new(item.SourceCode),
            DisplayName = new(item.DisplayName)
        })];
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
            ImplementationsLocation = implementationLocation,
            AccessSpecifiers = node.AccessSpecifiers
        };
    }

    /// <inheritdoc />
    public async Task<Result> LinkDependencyAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location nodeLocation, Models.Analyze.CXX.Location fromLocation)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Result<Guid> NodeId = await GetNodeIdFromLocationCanCrossProjectAsync(projectId, vcProjectName, nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail($"Node location not found. Start line: {nodeLocation.StartLine}, End Line: {nodeLocation.EndLine}, File: {nodeLocation.FilePath}");

        Result<Guid> FromId = await GetNodeIdFromLocationCanCrossProjectAsync(projectId, vcProjectName, fromLocation);
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
        Result<Guid> NodeId = await GetNodeIdFromLocationCanCrossProjectAsync(projectId, vcProjectName, nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail($"Node location not found. Start line: {nodeLocation.StartLine}, End Line: {nodeLocation.EndLine}, File: {nodeLocation.FilePath}");

        int locationCount = await dbContext.Cxx_DefineLocations
                .Include(item => item.Node)
                .CountAsync(item => item.Node!.ProjectId == projectId && item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && item.StartColumn == fromLocation.StartColumn && item.EndLine == fromLocation.EndLine && item.EndColumn == fromLocation.EndColumn && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"));

        Guid fromId = Guid.Empty;
        if (locationCount > 1)
        {
            fromId = await dbContext.Cxx_DefineLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && item.StartColumn == fromLocation.StartColumn && item.EndLine == fromLocation.EndLine && item.EndColumn == fromLocation.EndColumn && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"))
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        }
        if (fromId == Guid.Empty && locationCount > 0)
        {
            fromId = await dbContext.Cxx_DefineLocations
                .Include(item => item.Node)
                .Where(item => item.Node!.ProjectId == projectId && item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && item.StartColumn == fromLocation.StartColumn && item.EndLine == fromLocation.EndLine && item.EndColumn == fromLocation.EndColumn && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"))
                .Select(item => item.NodeId)
                .FirstOrDefaultAsync();
        }

        if (fromId == Guid.Empty)
        {
            locationCount = await dbContext.Cxx_ImplementationLocations
                .Include(item => item.Node)
                .CountAsync(item => item.Node!.ProjectId == projectId && item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && item.StartColumn == fromLocation.StartColumn && item.EndLine == fromLocation.EndLine && item.EndColumn == fromLocation.EndColumn && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"));

            if (locationCount > 1)
            {
                fromId = await dbContext.Cxx_ImplementationLocations
                    .Include(item => item.Node)
                    .Where(item => item.Node!.ProjectId == projectId && item.Node!.VcProjectName == vcProjectName && item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && item.StartColumn == fromLocation.StartColumn && item.EndLine == fromLocation.EndLine && item.EndColumn == fromLocation.EndColumn && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"))
                    .Select(item => item.NodeId)
                    .FirstOrDefaultAsync();
            }
            if (fromId == Guid.Empty && locationCount > 0)
            {
                fromId = await dbContext.Cxx_ImplementationLocations
                    .Include(item => item.Node)
                    .Where(item => item.Node!.ProjectId == projectId && item.FilePath == fromLocation.FilePath && item.StartLine == fromLocation.StartLine && item.StartColumn == fromLocation.StartColumn && item.EndLine == fromLocation.EndLine && item.EndColumn == fromLocation.EndColumn && (item.Node!.CursorKindSpelling == "ClassDecl" || item.Node!.CursorKindSpelling == "StructDecl"))
                    .Select(item => item.NodeId)
                    .FirstOrDefaultAsync();
            }
        }

        if (fromId == Guid.Empty)
            return Result.Fail($"From location not found. Start line: {fromLocation.StartLine}, End Line: {fromLocation.EndLine}, File: {fromLocation.FilePath}");

        if (await dbContext.Cxx_NodeDependencies.AnyAsync(item => item.NodeId == NodeId.Value && item.FromId == fromId))
            return Result.Ok();

        await dbContext.Cxx_NodeDependencies.AddAsync(new() { NodeId = NodeId.Value, FromId = fromId });
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

        if (await dbContext.Cxx_NodeMembers.AnyAsync(item => item.NodeId == NodeId.Value && item.MemberId == memberId))
            return Result.Ok();
        await dbContext.Cxx_NodeMembers.AddAsync(new() { NodeId = NodeId.Value, MemberId = memberId });
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    /// <inheritdoc />
    public async Task<Result> LinkTypeAsync(Guid projectId, string vcProjectName, Models.Analyze.CXX.Location nodeLocation, Models.Analyze.CXX.Location typeLocation)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Result<Guid> NodeId = await GetNodeIdFromLocationCanCrossProjectAsync(projectId, vcProjectName, nodeLocation);
        if (NodeId.IsFailed)
            return Result.Fail($"Node location not found. Start line: {nodeLocation.StartLine}, End Line: {nodeLocation.EndLine}, File: {nodeLocation.FilePath}");

        Result<Guid> TypeId = await GetNodeIdFromLocationCanCrossProjectAsync(projectId, vcProjectName, typeLocation);
        if (TypeId.IsFailed)
            return Result.Fail($"Type location not found. Start line: {typeLocation.StartLine}, End Line: {typeLocation.EndLine}, File: {typeLocation.FilePath}");

        // 因為在 cpp 實作時，需要將 class name 代入(bool Channel::port_in_use())，這時會有一個 TypeRef 指向 Channel
        // 實際上 port_in_use 是 Channel 的 member，所以這邊要檢查是否已經有 member 的關聯，如果有就不需要再建立 Type 關聯
        if (dbContext.Cxx_NodeMembers.Any(item => item.NodeId == TypeId.Value && item.MemberId == NodeId.Value))
            return Result.Ok();

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
            .Where(item => item.ProjectId == projectId && (item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl"))
            .Where(item => item.VcProjectName == nodeInfo.VcProjectName && item.Spelling == nodeInfo.Spelling && item.CxType == nodeInfo.CxType && item.NameSpace == nodeInfo.NameSpace)
            .Where(item => item.Members.Count > 0)
            .Select(item => item.Id)
            .ToArrayAsync();
        Guid[] noMembers = await dbContext.Cxx_Nodes.Include(item => item.Members)
            .Where(item => item.ProjectId == projectId && (item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl"))
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
        Node[] nodes = await dbContext.Cxx_Nodes.Where(item => item.ProjectId == projectId && item.VcProjectName == vcProjectName && (item.CursorKindSpelling == "ClassDecl" || item.CursorKindSpelling == "StructDecl")).ToArrayAsync();

        List<Guid> removeIds = [.. nodes.Select(n => n.Id)];
        foreach (Node node in nodes)
        {
            bool hasMember = await dbContext.Cxx_NodeMembers
                .AnyAsync(m => m.NodeId == node.Id || m.MemberId == node.Id);
            bool hasDep = await dbContext.Cxx_NodeDependencies
                .AnyAsync(d => d.FromId == node.Id || d.NodeId == node.Id);
            bool hasType = await dbContext.Cxx_NodeTypes
                .AnyAsync(t => t.TypeId == node.Id || t.NodeId == node.Id);

            if (hasMember || hasDep || hasType)
                removeIds.Remove(node.Id);
        }
        Node[] toRemove = nodes.Where(n => removeIds.Contains(n.Id)).ToArray();
        dbContext.Cxx_Nodes.RemoveRange(toRemove);
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
                StartColumn = node.DefineLocation.StartColumn,
                EndLine = node.DefineLocation.EndLine,
                EndColumn = node.DefineLocation.EndColumn,
                SourceCode = node.DefineLocation.SourceCode?.Value,
                DisplayName = node.DefineLocation.DisplayName?.Value,
                Node = dbNode
            };
            await dbContext.Cxx_DefineLocations.AddAsync(location);
        }
        else if (defineLocation is not null && node.DefineLocation is not null)
        {
            defineLocation.FilePath = node.DefineLocation!.FilePath;
            defineLocation.StartLine = node.DefineLocation!.StartLine;
            defineLocation.StartColumn = node.DefineLocation!.StartColumn;
            defineLocation.EndLine = node.DefineLocation!.EndLine;
            defineLocation.EndColumn = node.DefineLocation!.EndColumn;
            defineLocation.SourceCode = node.DefineLocation!.SourceCode?.Value;
            defineLocation.DisplayName = node.DefineLocation!.DisplayName?.Value;
        }
        else if (defineLocation is not null && node.DefineLocation is null)
        {
            dbContext.Cxx_DefineLocations.Remove(defineLocation);
        }

        ICollection<ImplementationLocation> implementationsLocation = await dbContext.Cxx_ImplementationLocations.Where(item => item.NodeId == node.NodeId).ToArrayAsync();
        HashSet<string> dbImplementationsLocationHash = [.. implementationsLocation.AsParallel().Select(item => item.ComputeSHA256Hash())];
        HashSet<string> newImplementationsLocationHash = [.. node.ImplementationsLocation.AsParallel().Select(item => item.ComputeSHA256Hash())];

        ICollection<ImplementationLocation> needRemove = [.. implementationsLocation.AsParallel().Where(item => !newImplementationsLocationHash.Contains(item.ComputeSHA256Hash()))];
        if (needRemove.Count > 0)
            dbContext.Cxx_ImplementationLocations.RemoveRange(needRemove);
        HashSet<Models.Analyze.CXX.Location> needAdd = [.. node.ImplementationsLocation.AsParallel().Where(item => !dbImplementationsLocationHash.Contains(item.ComputeSHA256Hash()))];
        if (needAdd.Count > 0)
        {
            await dbContext.Cxx_ImplementationLocations.AddRangeAsync(needAdd.Select(item => new ImplementationLocation
            {
                FilePath = item.FilePath,
                StartLine = item.StartLine,
                StartColumn = item.StartColumn,
                EndLine = item.EndLine,
                EndColumn = item.EndColumn,
                SourceCode = item.SourceCode?.Value,
                DisplayName = item.DisplayName?.Value,
                Node = dbNode
            }));
        }

        await dbContext.SaveChangesAsync();
        return Result.Ok();
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

    public async Task<Result> UpdateNodeAsync(Models.Analyze.CXX.Node node)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? dbNode = await dbContext.Cxx_Nodes.FindAsync(node.Id);
        if (dbNode is null)
            return Result.Fail("Node not found");
        dbNode.VcProjectName = node.VcProjectName;
        dbNode.CursorKindSpelling = node.CursorKindSpelling;
        dbNode.Spelling = node.Spelling;
        dbNode.CxType = node.CxType;
        dbNode.NameSpace = node.NameSpace;
        dbContext.Cxx_Nodes.Update(dbNode);
        await dbContext.SaveChangesAsync();
        return Result.Ok();
    }

    public async Task<Result<Guid[]>> GetNodeDependenciesIdAsync(Guid nodeId)
    {
        ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        string? cursorKindSpelling = await dbContext.Cxx_Nodes
            .Where(item => item.Id == nodeId)
            .Select(item => item.CursorKindSpelling)
            .FirstOrDefaultAsync();
        await dbContext.DisposeAsync();
        if (string.IsNullOrEmpty(cursorKindSpelling))
            return Result.Fail<Guid[]>("Node not found");
        if (cursorKindSpelling == "ClassDecl" || cursorKindSpelling == "StructDecl" || cursorKindSpelling == "ClassTemplate")
        {
            dbContext = await dbContextFactory.CreateDbContextAsync();
            Guid[] members = await dbContext.Cxx_NodeMembers
                .Where(item => item.NodeId == nodeId)
                .Select(item => item.MemberId)
                .ToArrayAsync();
            await dbContext.DisposeAsync();
            ConcurrentBag<Result<Guid[]>> dependencies = [];
            await Parallel.ForEachAsync(members, async (member, _) =>
            {
                Result<Guid[]> result = await GetNodeDependenciesIdAsync(member);
                dependencies.Add(result);
            });
            return dependencies.AsParallel().Where(item => item.IsSuccess).SelectMany(item => item.Value).Distinct().Where(item => !members.Contains(item)).ToArray();
        }

        ArborisDbContext dbContext1 = await dbContextFactory.CreateDbContextAsync();
        Task<Guid[]> dependenciesTask = dbContext1.Cxx_NodeDependencies
            .Where(item => item.NodeId == nodeId)
            .Select(item => item.FromId)
            .ToArrayAsync();
        ArborisDbContext dbContext2 = await dbContextFactory.CreateDbContextAsync();
        Task<Guid[]> typesTask = dbContext2.Cxx_NodeTypes
            .Where(item => item.NodeId == nodeId)
            .Select(item => item.TypeId)
            .ToArrayAsync();

        Guid[][] result = await Task.WhenAll(dependenciesTask, typesTask);
        await Task.WhenAll(dbContext1.DisposeAsync().AsTask(), dbContext2.DisposeAsync().AsTask());
        return result.AsParallel().SelectMany(item => item).Distinct().ToArray();
    }

    public async Task<Result<(string? NameSpace, string? Spelling, string? AccessSpecifiers, Guid? ClassNodeId, string? CursorKindSpelling, bool NeedGenerate, string VcProjectName)>> GetNodeInfoWithClassIdAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes.Include(item => item.ImplementationsLocation).FirstOrDefaultAsync(item => item.Id == nodeId);

        if (node is null)
            return Result.Fail<(string? NameSpace, string? Spelling, string? AccessSpecifiers, Guid? ClassNodeId, string? CursorKindSpelling, bool NeedGenerate, string VcProjectName)>("Node not found");

        Guid classNodeId = await dbContext.Cxx_NodeMembers
            .Where(item => item.MemberId == nodeId)
            .Select(item => item.NodeId)
            .FirstOrDefaultAsync();

        return (node.NameSpace, node.Spelling, node.AccessSpecifiers, classNodeId == Guid.Empty ? null : classNodeId, node.CursorKindSpelling, node.ImplementationsLocation.Count > 0, node.VcProjectName);
    }

    public async Task<Result<NodeSourceCode[]>> GetNodeSourceCodeAsync(Guid nodeId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Node? node = await dbContext.Cxx_Nodes
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationsLocation)
            .FirstOrDefaultAsync(item => item.Id == nodeId);

        if (node is null)
            return Result.Fail<NodeSourceCode[]>("Node not found");

        List<NodeSourceCode> sourceCodes = [];
        if (node.DefineLocation is not null)
            sourceCodes.Add(new NodeSourceCode(node.DefineLocation.FilePath, node.DefineLocation.DisplayName, node.DefineLocation.SourceCode, true));
        if (node.ImplementationsLocation.Count > 0)
            sourceCodes.AddRange(node.ImplementationsLocation.Select(item => new NodeSourceCode(item.FilePath, item.DisplayName, item.SourceCode, false)));

        return sourceCodes.ToArray();
    }

    public async Task<Result<GetAllNodeDto[]>> GetAllNodeAsync(Guid projectId)
    {
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        Project? project = await dbContext.Projects.Include(item => item.CxxNodes).ThenInclude(item => item.ImplementationsLocation).FirstOrDefaultAsync(item => item.Id == projectId);
        if (project is null)
            return Result.Fail("Project not found");
        return project.CxxNodes!.AsParallel().Select(item => new GetAllNodeDto(item.Id, item.CursorKindSpelling, item.ImplementationsLocation.Count > 0)).ToArray();
    }

    public async Task<Result<NodeLines>> GetNodeAndLineStringFromFile(Guid projectId, string filePath, int line)
    {
        filePath = filePath.Replace("\\", "/");
        using ArborisDbContext dbContext = await dbContextFactory.CreateDbContextAsync();
        NodeLines? result = await dbContext.Cxx_DefineLocations
            .Include(item => item.Node)
            .Where(item => EF.Functions.Like(item.FilePath, filePath) && line >= item.StartLine && line <= item.EndLine && item.Node!.ProjectId == projectId)
            .Select(item => new NodeLines(item.NodeId, item.StartLine, item.EndLine, item.SourceCode)).FirstOrDefaultAsync();
        if (result is not null)
            return Result.Ok(result);
        result = await dbContext.Cxx_ImplementationLocations
            .Include(item => item.Node)
            .Where(item => EF.Functions.Like(item.FilePath, filePath) && line >= item.StartLine && line <= item.EndLine && item.Node!.ProjectId == projectId)
            .Select(item => new NodeLines(item.NodeId, item.StartLine, item.EndLine, item.SourceCode)).FirstOrDefaultAsync();
        if (result is not null)
            return Result.Ok(result);
        return Result.Fail<NodeLines>($"No node found in file: {filePath} at line: {line} for project: {projectId}");
    }
}
