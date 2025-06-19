using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;

namespace Arboris.Tests.EntityFramework.Repositories.CxxRepositoryTests;

[TestClass]
public class UpdateNodeLocationAsyncTests
{
    private IDbContextFactory<ArborisDbContext> dbFactory = null!;
    private GenerateBuilder generateBuilder = null!;
    private CxxRepository cxxRepository = null!;
    private ArborisDbContext db = null!;
    private readonly Models.Analyze.CXX.Location TestLocation;

    public UpdateNodeLocationAsyncTests()
        => TestLocation = new("TestUpdate.cpp", 100, 0, 200, 0);

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
    public async Task TestUpdateImplementationLocationIfIsNull()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateRootNode1();

        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
        Models.Analyze.CXX.NodeWithLocationDto dto = new(generateBuilder.Nodes[0].Id, generateBuilder.Nodes[0].DefineLocation, [TestLocation]);
        await cxxRepository.UpdateNodeLocationAsync(dto);
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());

        Node node = await db.Cxx_Nodes.AsNoTracking().Include(item => item.DefineLocation).Include(item => item.ImplementationsLocation).FirstAsync();
        Assert.AreEqual(TestLocation.FilePath, node.ImplementationsLocation.First().FilePath);
        Assert.AreEqual(TestLocation.StartLine, node.ImplementationsLocation.First().StartLine);
        Assert.AreEqual(TestLocation.EndLine, node.ImplementationsLocation.First().EndLine);
    }

    [TestMethod]
    public async Task TestUpdateDefineLocationIfIsNull()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateProject2()
            .GenerateRootNode2();

        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
        Models.Analyze.CXX.NodeWithLocationDto dto = new(generateBuilder.Nodes[0].Id, TestLocation, generateBuilder.Nodes[0].ImplementationsLocation);
        await cxxRepository.UpdateNodeLocationAsync(dto);
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());

        Node node = await db.Cxx_Nodes.AsNoTracking().Include(item => item.DefineLocation).Include(item => item.ImplementationsLocation).FirstAsync();
        Assert.AreEqual(TestLocation.FilePath, node.DefineLocation!.FilePath);
        Assert.AreEqual(TestLocation.StartLine, node.DefineLocation.StartLine);
        Assert.AreEqual(TestLocation.EndLine, node.DefineLocation.EndLine);
    }

    [TestMethod]
    public async Task TestUpdateDefineLocation()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateRootNode1();

        Node node = await db.Cxx_Nodes.AsNoTracking().Include(item => item.DefineLocation).Include(item => item.ImplementationsLocation).FirstAsync();
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, node.DefineLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, node.DefineLocation.StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, node.DefineLocation.EndLine);
        Models.Analyze.CXX.NodeWithLocationDto dto = new(generateBuilder.Nodes[0].Id, TestLocation, []);
        await cxxRepository.UpdateNodeLocationAsync(dto);
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
        node = await db.Cxx_Nodes.AsNoTracking().Include(item => item.DefineLocation).Include(item => item.ImplementationsLocation).FirstAsync();
        Assert.AreEqual(TestLocation.FilePath, node.DefineLocation!.FilePath);
        Assert.AreEqual(TestLocation.StartLine, node.DefineLocation.StartLine);
        Assert.AreEqual(TestLocation.EndLine, node.DefineLocation.EndLine);
    }

    [TestMethod]
    public async Task TestUpdateImplementationLocation()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateProject2()
            .GenerateRootNode2();

        Node node = await db.Cxx_Nodes.AsNoTracking().Include(item => item.DefineLocation).Include(item => item.ImplementationsLocation).FirstAsync();
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, node.ImplementationsLocation.First().FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, node.ImplementationsLocation.First().StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, node.ImplementationsLocation.First().EndLine);
        Models.Analyze.CXX.NodeWithLocationDto dto = new(generateBuilder.Nodes[0].Id, null, [TestLocation]);
        await cxxRepository.UpdateNodeLocationAsync(dto);
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
        node = await db.Cxx_Nodes.AsNoTracking().Include(item => item.DefineLocation).Include(item => item.ImplementationsLocation).FirstAsync();
        Assert.AreEqual(TestLocation.FilePath, node.ImplementationsLocation.First().FilePath);
        Assert.AreEqual(TestLocation.StartLine, node.ImplementationsLocation.First().StartLine);
        Assert.AreEqual(TestLocation.EndLine, node.ImplementationsLocation.First().EndLine);
    }

    [TestMethod]
    public async Task TestRemoveDefineLocation()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateRootNode1();

        Node node = await db.Cxx_Nodes.AsNoTracking().Include(item => item.DefineLocation).Include(item => item.ImplementationsLocation).FirstAsync();
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, node.DefineLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, node.DefineLocation.StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, node.DefineLocation.EndLine);
        Models.Analyze.CXX.NodeWithLocationDto dto = new(generateBuilder.Nodes[0].Id, null, []);
        await cxxRepository.UpdateNodeLocationAsync(dto);
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
    }

    [TestMethod]
    public async Task TestRemoveImplementationLocation()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateProject2()
            .GenerateRootNode2();

        Node node = await db.Cxx_Nodes.AsNoTracking().Include(item => item.DefineLocation).Include(item => item.ImplementationsLocation).FirstAsync();
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(1, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, node.ImplementationsLocation.First().FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, node.ImplementationsLocation.First().StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, node.ImplementationsLocation.First().EndLine);
        Models.Analyze.CXX.NodeWithLocationDto dto = new(generateBuilder.Nodes[0].Id, null, []);
        await cxxRepository.UpdateNodeLocationAsync(dto);
        Assert.AreEqual(1, await db.Cxx_Nodes.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_DefineLocations.AsNoTracking().CountAsync());
        Assert.AreEqual(0, await db.Cxx_ImplementationLocations.AsNoTracking().CountAsync());
    }

    [TestMethod]
    public async Task TestGetNodeFromDefineLocationAsync()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateProject2()
            .GenerateRootNode1()
            .GenerateRootNode2();

        Result<Models.Analyze.CXX.Node> node = await cxxRepository.GetNodeFromDefineLocationAsync(generateBuilder.Nodes[0].ProjectId, generateBuilder.Nodes[0].VcProjectName, generateBuilder.Nodes[0].DefineLocation!);
        Assert.IsTrue(node.IsSuccess);
        Assert.AreEqual(generateBuilder.Nodes[0].Id, node.Value.Id);
        Assert.AreEqual(generateBuilder.Nodes[0].VcProjectName, node.Value.VcProjectName);
        Assert.AreEqual(generateBuilder.Nodes[0].CursorKindSpelling, node.Value.CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[0].CxType, node.Value.CxType);
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, node.Value.Spelling);
        Assert.AreEqual(generateBuilder.Nodes[0].NameSpace, node.Value.NameSpace);
        Assert.AreEqual(generateBuilder.Nodes[0].DefineLocation!.FilePath, node.Value.DefineLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Nodes[0].DefineLocation!.StartLine, node.Value.DefineLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Nodes[0].DefineLocation!.EndLine, node.Value.DefineLocation!.EndLine);
        Assert.AreEqual(0, node.Value.ImplementationsLocation.Count);
        Models.Analyze.CXX.NodeWithLocationDto dto = new(generateBuilder.Nodes[0].Id, generateBuilder.Nodes[0].DefineLocation, [TestLocation]);
        await cxxRepository.UpdateNodeLocationAsync(dto);
        node = await cxxRepository.GetNodeFromDefineLocationAsync(generateBuilder.Nodes[0].ProjectId, generateBuilder.Nodes[0].VcProjectName, generateBuilder.Nodes[0].DefineLocation!);
        Assert.AreEqual(TestLocation.FilePath, node.Value.ImplementationsLocation.First().FilePath);
        Assert.AreEqual(TestLocation.StartLine, node.Value.ImplementationsLocation.First().StartLine);
        Assert.AreEqual(TestLocation.EndLine, node.Value.ImplementationsLocation.First().EndLine);
    }
}
