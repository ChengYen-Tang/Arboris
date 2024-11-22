using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;
using Arboris.Tests.EntityFramework.Repositories.TestData.Generate;

namespace Arboris.Tests.EntityFramework.Repositories.CxxRepositoryTests;

[TestClass]
public class AddNodeAsyncTests
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
    public async Task TestAddOneNode()
    {
        generateBuilder.GenerateProject1();

        AddNode addNode = Generator.GenetateAddNodeWithDefineLocation(generateBuilder.Projects[0].Id);
        await cxxRepository.AddNodeAsync(addNode);

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
    public async Task TestAddTwoNode()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateProject2();

        AddNode node1 = Generator.GenetateAddNodeWithDefineLocation(generateBuilder.Projects[0].Id);
        AddNode node2 = Generator.GenetateAddNodeWithImplementationLocation(generateBuilder.Projects[1].Id);
        await cxxRepository.AddNodeAsync(node1);
        await cxxRepository.AddNodeAsync(node2);

        ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(2, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.CountAsync());
        Assert.AreEqual(1, await db.Cxx_ImplementationLocations.CountAsync());
        db.Projects.Remove(generateBuilder.Projects[0]);
        await db.SaveChangesAsync();
        await db.DisposeAsync();
        db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(1, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(0, await db.Cxx_DefineLocations.CountAsync());
        Assert.AreEqual(1, await db.Cxx_ImplementationLocations.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.DefineLocation).Include(item => item.ImplementationLocation).FirstAsync();
        Assert.AreEqual(node2.ProjectId, node.ProjectId);
        Assert.AreEqual(node2.VcProjectName, node.VcProjectName);
        Assert.AreEqual(node2.CursorKindSpelling, node.CursorKindSpelling);
        Assert.AreEqual(node2.CxType, node.CxType);
        Assert.AreEqual(node2.Spelling, node.Spelling);
        Assert.AreEqual(node2.NameSpace, node.NameSpace);
        Assert.IsNull(node.DefineLocation);
        Assert.AreEqual(node2.ImplementationLocation!.FilePath, node.ImplementationLocation!.FilePath);
        Assert.AreEqual(node2.ImplementationLocation.StartLine, node.ImplementationLocation.StartLine);
        Assert.AreEqual(node2.ImplementationLocation.EndLine, node.ImplementationLocation.EndLine);
        await db.DisposeAsync();
    }

    [TestMethod]
    public async Task TestGetNodesFromProjectAsync()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateProject2()
            .GenerateRootNode1()
            .GenerateRootNode2();

        Models.Analyze.CXX.NodeInfoWithLocation[] nodes = await cxxRepository.GetNodesFromProjectAsync(generateBuilder.Projects[0].Id);
        Assert.AreEqual(1, nodes.Length);
        Assert.AreEqual(generateBuilder.Nodes[0].Id, nodes[0].Id);
        Assert.AreEqual(generateBuilder.Nodes[0].VcProjectName, nodes[0].VcProjectName);
        Assert.AreEqual(generateBuilder.Nodes[0].CursorKindSpelling, nodes[0].CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[0].CxType, nodes[0].CxType);
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, nodes[0].Spelling);
        Assert.AreEqual(generateBuilder.Nodes[0].NameSpace, nodes[0].NameSpace);
        Assert.AreEqual(generateBuilder.Nodes[0].DefineLocation!.FilePath, nodes[0].DefineLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Nodes[0].DefineLocation!.StartLine, nodes[0].DefineLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Nodes[0].DefineLocation!.EndLine, nodes[0].DefineLocation!.EndLine);
        nodes = await cxxRepository.GetNodesFromProjectAsync(generateBuilder.Projects[1].Id);
        Assert.AreEqual(1, nodes.Length);
        Assert.AreEqual(generateBuilder.Nodes[1].Id, nodes[0].Id);
        Assert.AreEqual(generateBuilder.Nodes[1].VcProjectName, nodes[0].VcProjectName);
        Assert.AreEqual(generateBuilder.Nodes[1].CursorKindSpelling, nodes[0].CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[1].CxType, nodes[0].CxType);
        Assert.AreEqual(generateBuilder.Nodes[1].Spelling, nodes[0].Spelling);
        Assert.AreEqual(generateBuilder.Nodes[1].NameSpace, nodes[0].NameSpace);
        Assert.AreEqual(generateBuilder.Nodes[1].ImplementationLocation!.FilePath, nodes[0].ImplementationLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Nodes[1].ImplementationLocation!.StartLine, nodes[0].ImplementationLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Nodes[1].ImplementationLocation!.EndLine, nodes[0].ImplementationLocation!.EndLine);
    }

    [TestMethod]
    public async Task TestGetNodeAsync()
    {
        generateBuilder.GenerateProject1().GenerateProject2();

        AddNode node1 = Generator.GenetateAddNodeWithDefineLocation(generateBuilder.Projects[0].Id);
        AddNode node2 = Generator.GenetateAddNodeWithImplementationLocation(generateBuilder.Projects[1].Id);
        await cxxRepository.AddNodeAsync(node1);
        await cxxRepository.AddNodeAsync(node2);


        Result<Guid> nodeId = await cxxRepository.CheckDefineNodeExistsAsync(node1);
        Assert.IsTrue(nodeId.IsSuccess);
        Assert.IsTrue(await cxxRepository.CheckImplementationNodeExistsAsync(node2));
        Result<Models.Analyze.CXX.Node> result = await cxxRepository.GetNodeAsync(nodeId.Value);
        Assert.IsTrue(result.IsSuccess);
        Models.Analyze.CXX.Node node = result.Value;
        Assert.AreEqual(node1.ProjectId, node.ProjectId);
        Assert.AreEqual(node1.VcProjectName, node.VcProjectName);
        Assert.AreEqual(node1.CursorKindSpelling, node.CursorKindSpelling);
        Assert.AreEqual(node1.CxType, node.CxType);
        Assert.AreEqual(node1.Spelling, node.Spelling);
        Assert.AreEqual(node1.NameSpace, node.NameSpace);
        Assert.IsNull(node.ImplementationLocation);
        Assert.AreEqual(node1.DefineLocation!.FilePath, node.DefineLocation!.FilePath);
        Assert.AreEqual(node1.DefineLocation.StartLine, node.DefineLocation.StartLine);
        Assert.AreEqual(node1.DefineLocation.EndLine, node.DefineLocation.EndLine);
    }
}
