using Arboris.Models.Graph.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;

namespace Arboris.Tests.EntityFramework.Repositories.CxxRepositoryTests;

[TestClass]
public class GetOverViewNodeTests
{
    private IDbContextFactory<ArborisDbContext> dbFactory = null!;
    private GenerateBuilder generateBuilder = null!;
    private CxxRepository cxxRepository = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        dbFactory = await DBContextInit.GetArborisDbContextFactoryAsync();
        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        cxxRepository = new(dbFactory);
        generateBuilder = new(db, cxxRepository);

        generateBuilder.GenerateProject1().GenerateProject2()
            .GenerateRootNode1().GenerateRootNode2()
            .GenerateDependencyNode().GenerateDependencyNode2()
            .GenerateMemberNode().GenerateMemberNode2()
            .GenerateTypeNode().GenerateTypeNode2();

        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[2]);
        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[3]);
        await cxxRepository.LinkMemberAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Nodes[4].Id);
        await cxxRepository.LinkMemberAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Nodes[5].Id);
        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[6]);
        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[7]);

        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[0].Id, "RootNode1");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[1].Id, "RootNode2");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[2].Id, "DependencyNode1");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[3].Id, "DependencyNode2");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[4].Id, "MemberNode1");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[5].Id, "MemberNode2");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[6].Id, "TypeNode1");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[7].Id, "TypeNode2");
    }

    [TestMethod]
    public async Task TestGetNodeDependenciesAsync()
    {
        Result<OverViewNode[]> overViewNode = await cxxRepository.GetNodeDependenciesAsync(generateBuilder.Nodes[0].Id);
        Assert.IsTrue(overViewNode.IsSuccess);
        Assert.AreEqual(2, overViewNode.Value.Length);
        Assert.IsTrue(overViewNode.Value.Any(item => item.DisplayName == generateBuilder.Locations[2].DisplayName && item.Description == "DependencyNode1"));
        Assert.IsTrue(overViewNode.Value.Any(item => item.DisplayName == generateBuilder.Locations[3].DisplayName && item.Description == "DependencyNode2"));
    }

    [TestMethod]
    public async Task TestGetNodeMembersAsync()
    {
        Result<OverViewNode[]> overViewNode = await cxxRepository.GetNodeMembersAsync(generateBuilder.Nodes[0].Id);
        Assert.IsTrue(overViewNode.IsSuccess);
        Assert.AreEqual(2, overViewNode.Value.Length);
        Assert.IsTrue(overViewNode.Value.Any(item => item.DisplayName == generateBuilder.Locations[4].DisplayName && item.Description == "MemberNode1"));
        Assert.IsTrue(overViewNode.Value.Any(item => item.DisplayName == generateBuilder.Locations[5].DisplayName && item.Description == "MemberNode2"));
    }

    [TestMethod]
    public async Task TestGetNodeTypesAsync()
    {
        Result<OverViewNode[]> overViewNode = await cxxRepository.GetNodeTypesAsync(generateBuilder.Nodes[0].Id);
        Assert.IsTrue(overViewNode.IsSuccess);
        Assert.AreEqual(2, overViewNode.Value.Length);
        Assert.IsTrue(overViewNode.Value.Any(item => item.DisplayName == generateBuilder.Locations[6].DisplayName && item.Description == "TypeNode1"));
        Assert.IsTrue(overViewNode.Value.Any(item => item.DisplayName == generateBuilder.Locations[7].DisplayName && item.Description == "TypeNode2"));
    }
}
