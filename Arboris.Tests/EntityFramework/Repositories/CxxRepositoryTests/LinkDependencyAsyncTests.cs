using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;
using Arboris.Tests.EntityFramework.Repositories.TestData.Generate;

namespace Arboris.Tests.EntityFramework.Repositories.CxxRepositoryTests;

[TestClass]
public class LinkDependencyAsyncTests
{
    private IDbContextFactory<ArborisDbContext> dbFactory = null!;
    private GenerateBuilder generateBuilder = null!;
    private CxxRepository cxxRepository = null!;
    private ArborisDbContext db = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        dbFactory = await DBContextInit.GetArborisDbContextFactoryAsync();
        db = await dbFactory.CreateDbContextAsync();
        cxxRepository = new(dbFactory);
        generateBuilder = new(db, cxxRepository);
    }

    [TestCleanup]
    public void Cleanup()
        => db.Dispose();

    [TestMethod]
    public async Task TestLinkDependencyAsync()
    {
        generateBuilder.GenerateProject1().GenerateProject2()
            .GenerateRootNode1()
            .GenerateDependencyNode();

        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, ["Arboris"], generateBuilder.Locations[0], generateBuilder.Locations[1]);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(2, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await db.Cxx_NodeDependencies.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.Dependencies).ThenInclude(item => item.From).FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(1, node.Dependencies.Count);
        Assert.AreEqual(generateBuilder.Nodes[1].Id, node.Dependencies.First().From.Id);
    }

    [TestMethod]
    public async Task TestLinkDoubleDependencyAsync()
    {
        generateBuilder.GenerateProject1().GenerateProject2()
            .GenerateRootNode1().GenerateRootNode2()
            .GenerateDependencyNode().GenerateDependencyNode2();

        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, ["Arboris"], generateBuilder.Locations[0], generateBuilder.Locations[2]);
        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, ["Arboris"], generateBuilder.Locations[0], generateBuilder.Locations[3]);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(4, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(2, await db.Cxx_NodeDependencies.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.Dependencies).FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(2, node.Dependencies.Count);
        CollectionAssert.AreEquivalent(generateBuilder.Nodes[2..].Select(item => item.Id).ToArray(), node.Dependencies.Select(item => item.FromId).ToArray());
    }

    [TestMethod]
    public async Task TestLinkDependencyCallExprOperatorEqualAsync()
    {
        generateBuilder.GenerateProject1().GenerateProject2()
            .GenerateRootNode1()
            .GenerateDependencyNode();

        await cxxRepository.LinkDependencyCallExprOperatorEqualAsync(generateBuilder.Projects[0].Id, ["Arboris"], generateBuilder.Locations[0], generateBuilder.Locations[1]);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(2, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await db.Cxx_NodeDependencies.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.Dependencies).ThenInclude(item => item.From).FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(1, node.Dependencies.Count);
        Assert.AreEqual(generateBuilder.Nodes[1].Id, node.Dependencies.First().From.Id);

        AddNode addNode = Generator.GenerateAddNodeWithDependencyFunctionDeclNode(generateBuilder.Projects[0].Id);
        await cxxRepository.AddNodeAsync(addNode);
        Result result = await cxxRepository.LinkDependencyCallExprOperatorEqualAsync(generateBuilder.Projects[0].Id, ["Arboris"], generateBuilder.Locations[0], addNode.DefineLocation!);
        Assert.IsFalse(result.IsSuccess);
    }
}
