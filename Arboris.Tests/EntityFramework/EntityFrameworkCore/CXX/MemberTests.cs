using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.CXX.TestData;
using Microsoft.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.EntityFrameworkCore.CXX;

[TestClass]
public class MemberTests
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
    public async Task TestCreateMemberAsync()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddMember1()
            .AddMember2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Members)!
            .ThenInclude(item => item.Member)
            .FirstAsync();

        Assert.AreEqual(2, node.Members!.Count);
        List<Node> members = node.Members.Select(item => item.Member).ToList();
        CollectionAssert.AreEquivalent(generateBuilder.Nodes.Select(item => item.Id).ToArray()[1..], members.Select(item => item.Id).ToArray());
    }

    [TestMethod]
    public async Task TestDeleteRootNode()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddMember1()
            .AddMember2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Members)!
            .ThenInclude(item => item.Member)
            .FirstAsync();

        Assert.AreEqual(2, node.Members!.Count);

        dbContext.Cxx_Nodes.Remove(generateBuilder.Nodes[0]);
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(0, await dbContext.Cxx_NodeMembers.CountAsync());
    }

    [TestMethod]
    public async Task TestDeleteMember()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddMember1()
            .AddMember2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Members)!
            .ThenInclude(item => item.Member)
            .FirstAsync();

        Assert.AreEqual(2, node.Members!.Count);

        dbContext.Cxx_Nodes.Remove(generateBuilder.Nodes[1]);
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_NodeMembers.CountAsync());
    }

    [TestMethod]
    public async Task TestDeleteMemberLink()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddMember1()
            .AddMember2()
            .BuildAsync();

        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Members)!
            .ThenInclude(item => item.Member)
            .FirstAsync();

        Assert.AreEqual(2, node.Members!.Count);

        dbContext.Cxx_NodeMembers.Remove(await dbContext.Cxx_NodeMembers.FirstAsync());
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(3, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_NodeMembers.CountAsync());
    }
}
