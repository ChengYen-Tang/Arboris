using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Models.Graph.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;

namespace Arboris.Tests.EntityFramework.Repositories.CxxRepositoryTests;

[TestClass]
public class UpdateLLMDescriptionAsyncTests
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
    public async Task TestUpdateLLMDescriptionAsync()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateProject2()
            .GenerateRootNode1()
            .GenerateRootNode2();

        Node? node = await db.Cxx_Nodes.FindAsync(generateBuilder.Nodes[0].Id);
        Assert.IsTrue(string.IsNullOrEmpty(node!.LLMDescription));
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[0].Id, "TestLLMDescription");
        using ArborisDbContext dbContext = await dbFactory.CreateDbContextAsync();
        node = await dbContext.Cxx_Nodes.FindAsync(generateBuilder.Nodes[0].Id);
        Assert.AreEqual("TestLLMDescription", node!.LLMDescription);
        node = await dbContext.Cxx_Nodes.FindAsync(generateBuilder.Nodes[1].Id);
        Assert.IsTrue(string.IsNullOrEmpty(node!.LLMDescription));
    }

    [TestMethod]
    public async Task TestGetForUnitTestNodeAsync()
    {
        generateBuilder
            .GenerateProject1()
            .GenerateProject2()
            .GenerateRootNode1()
            .GenerateRootNode2();

        Node? node = await db.Cxx_Nodes.FindAsync(generateBuilder.Nodes[0].Id);
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[0].Id, "TestLLMDescription");
        Result<OverViewNode> result = await cxxRepository.GetForUnitTestNodeAsync(generateBuilder.Nodes[0].Id);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual("TestLLMDescription", result.Value.Description);
        Assert.AreEqual(generateBuilder.Nodes[0].DefineLocation!.DisplayName, result.Value.DisplayName);
        result = await cxxRepository.GetForUnitTestNodeAsync(generateBuilder.Nodes[1].Id);
        Assert.IsTrue(string.IsNullOrEmpty(result.Value.Description));
        Assert.AreEqual(generateBuilder.Nodes[1].ImplementationLocation!.DisplayName, result.Value.DisplayName);
    }
}
