using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;
using Arboris.Tests.EntityFramework.Repositories.TestData.Generate;


namespace Arboris.Tests.Core.Aggregate.CxxAggregateTests;

[TestClass]
public class AddDefineNodeAsyncTests
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
    public async Task TestAddOneNode()
    {
        generateBuilder.GenerateProject1();

        AddNode addNode = Generator.GenetateAddNodeWithDefineLocation(generateBuilder.Projects[0].Id);
        await cxxAggregate.AddDefineNodeAsync(addNode);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(1, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.DefineLocation).Include(item => item.ImplementationLocation).FirstAsync();
        Assert.AreEqual(addNode.ProjectId, node.ProjectId);
        Assert.AreEqual(addNode.VcProjectName, node.VcProjectName);
        Assert.AreEqual(addNode.CursorKindSpelling, node.CursorKindSpelling);
        Assert.AreEqual(addNode.CxType, node.CxType);
        Assert.AreEqual(addNode.Spelling, node.Spelling);
        Assert.AreEqual(addNode.NameSpace, node.NameSpace);
        Assert.IsNull(node.ImplementationLocation);
        Assert.AreEqual(addNode.DefineLocation!.FilePath, node.DefineLocation!.FilePath);
        Assert.AreEqual(addNode.DefineLocation.StartLine, node.DefineLocation.StartLine);
        Assert.AreEqual(addNode.DefineLocation.EndLine, node.DefineLocation.EndLine);
    }

    [TestMethod]
    public async Task TestAddNodeRepeat()
    {
        generateBuilder
            .GenerateProject1();

        AddNode node1 = Generator.GenetateAddNodeWithDefineLocation(generateBuilder.Projects[0].Id);
        (Guid id, bool isExist) = await cxxAggregate.AddDefineNodeAsync(node1);
        Assert.IsFalse(isExist);
        (Guid id2, bool isExist2) = await cxxAggregate.AddDefineNodeAsync(node1);
        Assert.IsTrue(isExist2);
        Assert.AreEqual(id, id2);
    }
}
