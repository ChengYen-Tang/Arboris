using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.CXX.TestData;
using Microsoft.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.CXX;

[TestClass]
public class TypeTests
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
    public async Task TestCreateTypeAsync()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddType1()
            .AddType2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Types)!
            .ThenInclude(item => item.Type)
            .FirstAsync();

        Assert.AreEqual(2, node.Types!.Count);
        List<Node> types = node.Types.Select(item => item.Type).ToList();
        CollectionAssert.AreEquivalent(generateBuilder.Nodes.Select(item => item.Id).ToArray()[1..], types.Select(item => item.Id).ToArray());
    }

    [TestMethod]
    public async Task TestDeleteRootNode()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddType1()
            .AddType2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Types)!
            .ThenInclude(item => item.Type)
            .FirstAsync();

        Assert.AreEqual(2, node.Types!.Count);

        dbContext.Cxx_Nodes.Remove(generateBuilder.Nodes[0]);
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(0, await dbContext.Cxx_NodeTypes.CountAsync());
    }

    [TestMethod]
    public async Task TestDeleteType()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddType1()
            .AddType2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Types)!
            .ThenInclude(item => item.Type)
            .FirstAsync();

        Assert.AreEqual(2, node.Types!.Count);

        dbContext.Cxx_Nodes.Remove(generateBuilder.Nodes[1]);
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_NodeTypes.CountAsync());
    }

    [TestMethod]
    public async Task TestDeleteTypeLink()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddType1()
            .AddType2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Types)!
            .ThenInclude(item => item.Type)
            .FirstAsync();

        Assert.AreEqual(2, node.Types!.Count);

        dbContext.Cxx_NodeTypes.Remove(await dbContext.Cxx_NodeTypes.FirstAsync());
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(3, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_NodeTypes.CountAsync());
    }
}
