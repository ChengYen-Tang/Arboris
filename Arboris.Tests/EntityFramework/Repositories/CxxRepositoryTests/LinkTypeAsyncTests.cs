using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;

namespace Arboris.Tests.EntityFramework.Repositories.CxxRepositoryTests;

[TestClass]
public class LinkTypeAsyncTests
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
    public async Task TestLinkTypeAsync()
    {
        generateBuilder.GenerateProject1().GenerateRootNode1().GenerateTypeNode();

        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, ["Arboris"], generateBuilder.Locations[0], generateBuilder.Locations[1]);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(2, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await db.Cxx_NodeTypes.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.Types).ThenInclude(item => item.Type).FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(1, node.Types.Count);
        Assert.AreEqual(generateBuilder.Nodes[1].Id, node.Types.First().Type.Id);
    }

    [TestMethod]
    public async Task TestLinkDoubleTypesAsync()
    {
        generateBuilder.GenerateProject1().GenerateProject2()
            .GenerateRootNode1().GenerateRootNode2()
            .GenerateTypeNode().GenerateTypeNode2();

        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, ["Arboris"], generateBuilder.Locations[0], generateBuilder.Locations[2]);
        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, ["Arboris"], generateBuilder.Locations[0], generateBuilder.Locations[3]);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(4, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(2, await db.Cxx_NodeTypes.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.Types).FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(2, node.Types.Count);
        CollectionAssert.AreEquivalent(generateBuilder.Nodes[2..].Select(item => item.Id).ToArray(), node.Types.Select(item => item.TypeId).ToArray());
    }
}
