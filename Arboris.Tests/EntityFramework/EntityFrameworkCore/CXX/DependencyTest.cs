using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.CXX.TestData;

namespace Arboris.Tests.EntityFramework.EntityFrameworkCore.CXX;

[TestClass]
public class DependencyTest
{
    private IDbContextFactory<ArborisDbContext> dbFactory = null!;
    private GenerateBuilder generateBuilder = null!;
    private ArborisDbContext db = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        dbFactory = await DBContextInit.GetArborisDbContextFactoryAsync();
        db = await dbFactory.CreateDbContextAsync();
        generateBuilder = new(db);
    }

    [TestCleanup]
    public void Cleanup()
        => db.Dispose();

    [TestMethod]
    public async Task TestCreateDependencyAsync()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddDependency1()
            .AddDependency2()
            .BuildAsync();

        using ArborisDbContext dbContext = await dbFactory.CreateDbContextAsync();
        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Dependencies)!
            .ThenInclude(item => item.From)
            .FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);

        Assert.AreEqual(2, node.Dependencies!.Count);
        List<Node> dependencies = node.Dependencies.Select(item => item.From).ToList();
        CollectionAssert.AreEquivalent(generateBuilder.Nodes.Select(item => item.Id).ToArray()[1..], dependencies.Select(item => item.Id).ToArray());
    }

    [TestMethod]
    public async Task TestDeleteRootNode()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddDependency1()
            .AddDependency2()
            .BuildAsync();

        ArborisDbContext dbContext = await dbFactory.CreateDbContextAsync();
        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Dependencies)!
            .ThenInclude(item => item.From)
            .FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(2, node.Dependencies!.Count);
        await dbContext.DisposeAsync();

        dbContext = await dbFactory.CreateDbContextAsync();
        dbContext.Cxx_Nodes.Remove(generateBuilder.Nodes[0]);
        await dbContext.SaveChangesAsync();
        await dbContext.DisposeAsync();

        dbContext = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(0, await dbContext.Cxx_NodeDependencies.CountAsync());
        await dbContext.DisposeAsync();
    }

    [TestMethod]
    public async Task TestDeleteType()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddDependency1()
            .AddDependency2()
            .BuildAsync();

        ArborisDbContext dbContext = await dbFactory.CreateDbContextAsync();
        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Dependencies)!
            .ThenInclude(item => item.From)
            .FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(2, node.Dependencies!.Count);
        await dbContext.DisposeAsync();

        dbContext = await dbFactory.CreateDbContextAsync();
        dbContext.Cxx_Nodes.Remove(generateBuilder.Nodes[1]);
        await dbContext.SaveChangesAsync();
        await dbContext.DisposeAsync();

        dbContext = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_NodeDependencies.CountAsync());
        await dbContext.DisposeAsync();
    }

    [TestMethod]
    public async Task TestDeleteTypeLink()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddDependency1()
            .AddDependency2()
            .BuildAsync();

        using ArborisDbContext dbContext = await dbFactory.CreateDbContextAsync();
        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Dependencies)!
            .ThenInclude(item => item.From)
            .FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);

        Assert.AreEqual(2, node.Dependencies!.Count);

        dbContext.Cxx_NodeDependencies.Remove(await dbContext.Cxx_NodeDependencies.FirstAsync());
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(3, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_NodeDependencies.CountAsync());
    }
}
