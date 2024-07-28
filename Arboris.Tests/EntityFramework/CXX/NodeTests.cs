using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.CXX.TestData;
using Microsoft.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.CXX;

[TestClass]
public class NodeTests
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
    public async Task TestCreateNodeAsync()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .BuildAsync();

        Assert.AreEqual(1, await dbContext.Cxx_Nodes.CountAsync());
        Node node = await dbContext.Cxx_Nodes
            .Include(item => item.Project)
            .Include(item => item.HeaderLocation)
            .Include(item => item.CppLocation)
            .Include(item => item.HppLocation)
            .FirstAsync();
        Assert.AreEqual(generateBuilder.Nodes[0].Id, node.Id);
        Assert.AreEqual(generateBuilder.Nodes[0].CursorKindSpelling, node.CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, node.Spelling);
        Assert.AreEqual(generateBuilder.Nodes[0].ProjectId, node.ProjectId);
        Assert.AreEqual(1, await dbContext.Projects.Include(item => item.CxxNodes).Select(item => item.CxxNodes).CountAsync());
        Assert.AreEqual(generateBuilder.Projects[0].Id, node.Project!.Id);
        Assert.AreEqual(generateBuilder.Projects[0].Name, node.Project!.Name);
        Assert.AreEqual(generateBuilder.Projects[0].CreateTime, node.Project!.CreateTime);
        Assert.AreEqual(generateBuilder.Locations[0].Id, node.HeaderLocationId);
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, node.HeaderLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, node.HeaderLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, node.HeaderLocation!.EndLine);
        Assert.AreEqual(generateBuilder.Locations[1].Id, node.CppLocationId);
        Assert.AreEqual(generateBuilder.Locations[1].FilePath, node.CppLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[1].StartLine, node.CppLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[1].EndLine, node.CppLocation!.EndLine);
    }

    [TestMethod]
    public async Task TestDeleteProject()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateProject2()
            .GenerateRootNode1()
            .GenerateRootNode2()
            .BuildAsync();

        Assert.AreEqual(2, await dbContext.Projects.CountAsync());
        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(2, await dbContext.Cxx_HeaderLocations.CountAsync());
        Assert.AreEqual(2, await dbContext.Cxx_CppLocations.CountAsync());
        Node[] nodes = await dbContext.Cxx_Nodes
            .Include(item => item.Project)
            .Include(item => item.HeaderLocation)
            .Include(item => item.CppLocation)
            .Include(item => item.HppLocation)
            .ToArrayAsync();

        Assert.AreEqual(generateBuilder.Nodes[0].Id, nodes[0].Id);
        Assert.AreEqual(generateBuilder.Nodes[0].CursorKindSpelling, nodes[0].CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, nodes[0].Spelling);
        Assert.AreEqual(generateBuilder.Nodes[0].ProjectId, nodes[0].ProjectId);
        Assert.AreEqual(generateBuilder.Projects[0].Id, nodes[0].Project!.Id);
        Assert.AreEqual(generateBuilder.Projects[0].Name, nodes[0].Project!.Name);
        Assert.AreEqual(generateBuilder.Projects[0].CreateTime, nodes[0].Project!.CreateTime);
        Assert.AreEqual(generateBuilder.Locations[0].Id, nodes[0].HeaderLocationId);
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, nodes[0].HeaderLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, nodes[0].HeaderLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, nodes[0].HeaderLocation!.EndLine);
        Assert.AreEqual(generateBuilder.Locations[1].Id, nodes[0].CppLocationId);
        Assert.AreEqual(generateBuilder.Locations[1].FilePath, nodes[0].CppLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[1].StartLine, nodes[0].CppLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[1].EndLine, nodes[0].CppLocation!.EndLine);

        Assert.AreEqual(generateBuilder.Nodes[1].Id, nodes[1].Id);
        Assert.AreEqual(generateBuilder.Nodes[1].CursorKindSpelling, nodes[1].CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[1].Spelling, nodes[1].Spelling);
        Assert.AreEqual(generateBuilder.Nodes[1].ProjectId, nodes[1].ProjectId);
        Assert.AreEqual(generateBuilder.Projects[1].Id, nodes[1].Project!.Id);
        Assert.AreEqual(generateBuilder.Projects[1].Name, nodes[1].Project!.Name);
        Assert.AreEqual(generateBuilder.Projects[1].CreateTime, nodes[1].Project!.CreateTime);
        Assert.AreEqual(generateBuilder.Locations[2].Id, nodes[1].HeaderLocationId);
        Assert.AreEqual(generateBuilder.Locations[2].FilePath, nodes[1].HeaderLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[2].StartLine, nodes[1].HeaderLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[2].EndLine, nodes[1].HeaderLocation!.EndLine);
        Assert.AreEqual(generateBuilder.Locations[3].Id, nodes[1].CppLocationId);
        Assert.AreEqual(generateBuilder.Locations[3].FilePath, nodes[1].CppLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[3].StartLine, nodes[1].CppLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[3].EndLine, nodes[1].CppLocation!.EndLine);

        dbContext.Projects.Remove(nodes[1].Project!);
        await dbContext.SaveChangesAsync();
        Assert.AreEqual(1, await dbContext.Projects.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_HeaderLocations.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_CppLocations.CountAsync());
    }

    [TestMethod]
    public async Task RemoveLocation()
    {
        await generateBuilder
            .GenerateProject1()
            .GenerateRootNode1()
            .AddDependency1()
            .BuildAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_HeaderLocations.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_CppLocations.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_HppLocations.CountAsync());

        Node[] nodes = await dbContext.Cxx_Nodes
            .Include(item => item.Project)
            .Include(item => item.HeaderLocation)
            .Include(item => item.CppLocation)
            .Include(item => item.HppLocation)
            .ToArrayAsync();

        Node rootNode = nodes.First(item => item.Spelling == "RootNode1");
        Node depNode = nodes.First(item => item.Spelling == "DependencyNode");
        Assert.AreEqual(generateBuilder.Locations[2].Id, depNode.HppLocationId);
        Assert.AreEqual(generateBuilder.Locations[2].FilePath, depNode.HppLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[2].StartLine, depNode.HppLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[2].EndLine, depNode.HppLocation!.EndLine);

        dbContext.Cxx_HeaderLocations.Remove(rootNode.HeaderLocation!);
        dbContext.Cxx_CppLocations.Remove(rootNode.CppLocation!);
        dbContext.Cxx_HppLocations.Remove(depNode.HppLocation!);
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(0, await dbContext.Cxx_HeaderLocations.CountAsync());
        Assert.AreEqual(0, await dbContext.Cxx_CppLocations.CountAsync());
        Assert.AreEqual(0, await dbContext.Cxx_HppLocations.CountAsync());
    }
}
