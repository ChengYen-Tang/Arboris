using Arboris.Models.Graph.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;

namespace Arboris.Tests.EntityFramework.Repositories.CxxRepositoryTests;

[TestClass]
public class GetOverallTests
{
    private IDbContextFactory<ArborisDbContext> dbFactory = null!;
    private GenerateBuilder generateBuilder = null!;
    private CxxRepository cxxRepository = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        dbFactory = await DBContextInit.GetArborisDbContextFactoryAsync();
        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        cxxRepository = new(dbFactory);
        generateBuilder = new(db, cxxRepository);

        generateBuilder.GenerateProject1().GenerateProject2()
            .GenerateRootNode1().GenerateRootNode2()
            .GenerateDependencyNode().GenerateDependencyNode2()
            .GenerateMemberNode().GenerateMemberNode2()
            .GenerateTypeNode().GenerateTypeNode2();

        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[2]);
        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[3]);
        await cxxRepository.LinkMemberAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Nodes[4].Id);
        await cxxRepository.LinkMemberAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Nodes[5].Id);
        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[6]);
        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[7]);
    }

    [TestMethod]
    public async Task TestGetOverallNodeAsync()
    {
        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Result<OverallNode[]> nodes = await cxxRepository.GetOverallNodeAsync(generateBuilder.Projects[0].Id);
        Assert.IsTrue(nodes.IsSuccess);
        Assert.AreEqual(7, nodes.Value.Length);
        Assert.IsTrue(nodes.Value.Any(item => item.Id == generateBuilder.Nodes[0].Id && item.CursorKindSpelling == generateBuilder.Nodes[0].CursorKindSpelling));
        Assert.IsTrue(nodes.Value.Any(item => item.Id == generateBuilder.Nodes[2].Id && item.CursorKindSpelling == generateBuilder.Nodes[2].CursorKindSpelling));
        Assert.IsTrue(nodes.Value.Any(item => item.Id == generateBuilder.Nodes[3].Id && item.CursorKindSpelling == generateBuilder.Nodes[3].CursorKindSpelling));
        Assert.IsTrue(nodes.Value.Any(item => item.Id == generateBuilder.Nodes[4].Id && item.CursorKindSpelling == generateBuilder.Nodes[4].CursorKindSpelling));
        Assert.IsTrue(nodes.Value.Any(item => item.Id == generateBuilder.Nodes[5].Id && item.CursorKindSpelling == generateBuilder.Nodes[5].CursorKindSpelling));
        Assert.IsTrue(nodes.Value.Any(item => item.Id == generateBuilder.Nodes[6].Id && item.CursorKindSpelling == generateBuilder.Nodes[6].CursorKindSpelling));
        Assert.IsTrue(nodes.Value.Any(item => item.Id == generateBuilder.Nodes[7].Id && item.CursorKindSpelling == generateBuilder.Nodes[7].CursorKindSpelling));

        nodes = await cxxRepository.GetOverallNodeAsync(generateBuilder.Projects[1].Id);
        Assert.IsTrue(nodes.IsSuccess);
        Assert.AreEqual(1, nodes.Value.Length);
        Assert.IsTrue(nodes.Value.Any(item => item.Id == generateBuilder.Nodes[1].Id && item.CursorKindSpelling == generateBuilder.Nodes[1].CursorKindSpelling));
    }

    [TestMethod]
    public async Task TestGetOverallDependencyAsync()
    {
        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Result<OverallNodeDependency[]> dependencies = await cxxRepository.GetOverallNodeDependencyAsync(generateBuilder.Projects[0].Id);
        Assert.IsTrue(dependencies.IsSuccess);
        Assert.AreEqual(2, dependencies.Value.Length);
        Assert.IsTrue(dependencies.Value.Any(item => item.NodeId == generateBuilder.Nodes[0].Id && item.FromId == generateBuilder.Nodes[2].Id));
        Assert.IsTrue(dependencies.Value.Any(item => item.NodeId == generateBuilder.Nodes[0].Id && item.FromId == generateBuilder.Nodes[3].Id));
    }

    [TestMethod]
    public async Task TestGetOverallMemberAsync()
    {
        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Result<OverallNodeMember[]> members = await cxxRepository.GetOverallNodeMemberAsync(generateBuilder.Projects[0].Id);
        Assert.IsTrue(members.IsSuccess);
        Assert.AreEqual(2, members.Value.Length);
        Assert.IsTrue(members.Value.Any(item => item.NodeId == generateBuilder.Nodes[0].Id && item.MemberId == generateBuilder.Nodes[4].Id));
        Assert.IsTrue(members.Value.Any(item => item.NodeId == generateBuilder.Nodes[0].Id && item.MemberId == generateBuilder.Nodes[5].Id));
    }

    [TestMethod]
    public async Task TestGetOverallTypeAsync()
    {
        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Result<OverallNodeType[]> types = await cxxRepository.GetOverallNodeTypeAsync(generateBuilder.Projects[0].Id);
        Assert.IsTrue(types.IsSuccess);
        Assert.AreEqual(2, types.Value.Length);
        Assert.IsTrue(types.Value.Any(item => item.NodeId == generateBuilder.Nodes[0].Id && item.TypeId == generateBuilder.Nodes[6].Id));
        Assert.IsTrue(types.Value.Any(item => item.NodeId == generateBuilder.Nodes[0].Id && item.TypeId == generateBuilder.Nodes[7].Id));
    }
}
