using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.CXX.TestData;
using Microsoft.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.EntityFrameworkCore.CXX;

[TestClass]
public class DependencyTest
{
    private ArborisDbContext dbContext = null!;
    private GenerateBuilder generateBuilder = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        dbContext = await DBContextInit.GetArborisDbContextAsync();
        generateBuilder = new(dbContext);
    }

    [TestCleanup]
    public void Cleanup()
        => dbContext.Dispose();

    [TestMethod]
    public async Task TestCreateDependencyAsync()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddDependency1()
            .AddDependency2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Dependencies)!
            .ThenInclude(item => item.From)
            .FirstAsync();

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

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Dependencies)!
            .ThenInclude(item => item.From)
            .FirstAsync();

        Assert.AreEqual(2, node.Dependencies!.Count);

        dbContext.Cxx_Nodes.Remove(generateBuilder.Nodes[0]);
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(0, await dbContext.Cxx_NodeDependencies.CountAsync());
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

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Dependencies)!
            .ThenInclude(item => item.From)
            .FirstAsync();

        Assert.AreEqual(2, node.Dependencies!.Count);

        dbContext.Cxx_Nodes.Remove(generateBuilder.Nodes[1]);
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_NodeDependencies.CountAsync());
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

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Dependencies)!
            .ThenInclude(item => item.From)
            .FirstAsync();

        Assert.AreEqual(2, node.Dependencies!.Count);

        dbContext.Cxx_NodeDependencies.Remove(await dbContext.Cxx_NodeDependencies.FirstAsync());
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(3, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_NodeDependencies.CountAsync());
    }
}
