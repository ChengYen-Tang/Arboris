using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.Repositories.TestData;
using Arboris.Tests.EntityFramework.Repositories.TestData.Generate;

namespace Arboris.Tests.EntityFramework.Repositories.CxxRepositoryTests;

[TestClass]
public class OtherTests
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
            .GenerateMemberNode()
            .GenerateTypeNode().GenerateTypeNode2();

        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[2]);
        await cxxRepository.LinkDependencyAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[3]);
        await cxxRepository.LinkMemberAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Nodes[4].Id);
        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[5]);
        await cxxRepository.LinkTypeAsync(generateBuilder.Projects[0].Id, "Arboris", generateBuilder.Locations[0], generateBuilder.Locations[6]);

        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[0].Id, "RootNode1");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[1].Id, "RootNode2");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[2].Id, "DependencyNode1");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[3].Id, "DependencyNode2");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[4].Id, "MemberNode1");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[5].Id, "TypeNode1");
        await cxxRepository.UpdateLLMDescriptionAsync(generateBuilder.Nodes[6].Id, "TypeNode2");
    }

    [TestMethod]
    public async Task TestUpdateUserDescriptionAsync()
    {
        Result result = await cxxRepository.UpdateUserDescriptionAsync(generateBuilder.Projects[0].Id, generateBuilder.Nodes[0].VcProjectName, generateBuilder.Nodes[0].NameSpace, null, generateBuilder.Nodes[0].Spelling, generateBuilder.Nodes[0].CxType, "This is user description");
        Assert.IsTrue(result.IsSuccess);
        result = await cxxRepository.UpdateUserDescriptionAsync(generateBuilder.Projects[0].Id, generateBuilder.Nodes[4].VcProjectName, generateBuilder.Nodes[4].NameSpace, generateBuilder.Nodes[0].Spelling, generateBuilder.Nodes[4].Spelling, generateBuilder.Nodes[4].CxType, "This is user description(Member)");
        Assert.IsTrue(result.IsSuccess);
        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Node node = db.Cxx_Nodes.Find(generateBuilder.Nodes[0].Id)!;
        Assert.AreEqual("This is user description", node.UserDescription);
        node = db.Cxx_Nodes.Find(generateBuilder.Nodes[4].Id)!;
        Assert.AreEqual("This is user description(Member)", node.UserDescription);
    }

    [TestMethod]
    public async Task TestGetClassFromNodeAsync()
    {
        string? className = await cxxRepository.GetClassFromNodeAsync(generateBuilder.Nodes[4].Id);
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, className);
    }

    [TestMethod]
    public async Task TestGetDistinctClassAndStructNodeInfosAsync()
    {
        AddNode addNode = Generator.GenerateAddNodeWithDependencyFunctionDeclNode(generateBuilder.Projects[0].Id);
        await cxxRepository.AddNodeAsync(addNode);
        await cxxRepository.UpdateUserDescriptionAsync(generateBuilder.Projects[0].Id, generateBuilder.Nodes[0].VcProjectName, generateBuilder.Nodes[0].NameSpace, null, generateBuilder.Nodes[0].Spelling, generateBuilder.Nodes[0].CxType, "This is user description");
        Result<Models.Analyze.CXX.NodeInfo[]> result = await cxxRepository.GetDistinctClassAndStructNodeInfosAsync(generateBuilder.Projects[0].Id, generateBuilder.Nodes[0].VcProjectName);
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(3, result.Value.Length);
        Models.Analyze.CXX.NodeInfo nodeInfo = result.Value.FirstOrDefault(item => item.Id == generateBuilder.Nodes[0].Id)!;
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, nodeInfo.Spelling);
        Assert.AreEqual(generateBuilder.Nodes[0].CxType, nodeInfo.CxType);
        Assert.AreEqual(generateBuilder.Nodes[0].NameSpace, nodeInfo.NameSpace);
        Assert.AreEqual(generateBuilder.Nodes[0].CursorKindSpelling, nodeInfo.CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[0].VcProjectName, nodeInfo.VcProjectName);
        Assert.AreEqual("RootNode1", nodeInfo.LLMDescription);
        Assert.AreEqual("This is user description", nodeInfo.UserDescription);
    }

    [TestMethod]
    public async Task TestRemoveTypeDeclarations()
    {
        await cxxRepository.RemoveTypeDeclarations(generateBuilder.Projects[0].Id, generateBuilder.Nodes[0].VcProjectName);
        await cxxRepository.RemoveTypeDeclarations(generateBuilder.Projects[1].Id, generateBuilder.Nodes[1].VcProjectName);
        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(6, db.Cxx_Nodes.Count(item => item.ProjectId == generateBuilder.Projects[0].Id));
        Assert.AreEqual(0, db.Cxx_Nodes.Count(item => item.ProjectId == generateBuilder.Projects[1].Id));
    }
}
