using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;
using System.Collections.Concurrent;

namespace Arboris.Tests.Core.Aggregate.CxxAggregateTests;

[TestClass]
public class LinkMemberAsyncTests
{
    private IDbContextFactory<ArborisDbContext> dbFactory = null!;
    private GenerateBuilder generateBuilder = null!;
    private CxxRepository cxxRepository = null!;
    private CxxAggregate cxxAggregate = null!;
    private ArborisDbContext db = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        dbFactory = await DBContextInit.GetArborisDbContextFactoryAsync();
        db = await dbFactory.CreateDbContextAsync();
        cxxRepository = new(dbFactory);
        cxxAggregate = new(cxxRepository);
        generateBuilder = new(db, cxxRepository);
    }

    [TestCleanup]
    public void Cleanup()
        => db.Dispose();

    [TestMethod]
    public async Task TestLinkMemberAsync()
    {
        generateBuilder.GenerateProject1().GenerateRootNode1().GenerateMemberNode();

        Dictionary<Models.Analyze.CXX.Location, ConcurrentDictionary<Guid, IReadOnlyList<string>>> memberBuffer = new()
        {
            { generateBuilder.Locations[0], new ConcurrentDictionary<Guid, IReadOnlyList<string>>() }
        };
        memberBuffer[generateBuilder.Locations[0]].TryAdd(generateBuilder.Nodes[1].Id, ["Arboris"]);
        await cxxAggregate.LinkMemberAsync(generateBuilder.Projects[0].Id, memberBuffer);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(2, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await db.Cxx_NodeMembers.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.Members).ThenInclude(item => item.Member).FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(1, node.Members.Count);
        Assert.AreEqual(generateBuilder.Nodes[1].Id, node.Members.First().Member.Id);
    }

    [TestMethod]
    public async Task TestLinkDoubleMembersAsync()
    {
        generateBuilder.GenerateProject1().GenerateProject2()
            .GenerateRootNode1().GenerateRootNode2()
            .GenerateMemberNode().GenerateMemberNode2();

        Dictionary<Models.Analyze.CXX.Location, ConcurrentDictionary<Guid, IReadOnlyList<string>>> memberBuffer = new()
        {
            { generateBuilder.Locations[0], new ConcurrentDictionary<Guid, IReadOnlyList<string>>() }
        };
        memberBuffer[generateBuilder.Locations[0]].TryAdd(generateBuilder.Nodes[2].Id, ["Arboris"]);
        memberBuffer[generateBuilder.Locations[0]].TryAdd(generateBuilder.Nodes[3].Id, ["Arboris"]);
        await cxxAggregate.LinkMemberAsync(generateBuilder.Projects[0].Id, memberBuffer);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(4, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(2, await db.Cxx_NodeMembers.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.Members).FirstAsync(item => item.Id == generateBuilder.Nodes[0].Id);
        Assert.AreEqual(2, node.Members.Count);
        CollectionAssert.AreEquivalent(generateBuilder.Nodes[2..].Select(item => item.Id).ToArray(), node.Members.Select(item => item.MemberId).ToArray());
    }
}
