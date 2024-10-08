﻿using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.Tests.EntityFramework.CXX.TestData;
using Microsoft.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.EntityFrameworkCore.CXX;

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
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .FirstAsync();
        Assert.AreEqual(generateBuilder.Nodes[0].Id, node.Id);
        Assert.AreEqual(generateBuilder.Nodes[0].CursorKindSpelling, node.CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, node.Spelling);
        Assert.AreEqual(generateBuilder.Nodes[0].ProjectId, node.ProjectId);
        Assert.AreEqual(1, await dbContext.Projects.Include(item => item.CxxNodes).Select(item => item.CxxNodes).CountAsync());
        Assert.AreEqual(generateBuilder.Projects[0].Id, node.Project!.Id);
        Assert.AreEqual(generateBuilder.Projects[0].Name, node.Project!.Name);
        Assert.AreEqual(generateBuilder.Projects[0].CreateTime, node.Project!.CreateTime);
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, node.DefineLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, node.DefineLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, node.DefineLocation!.EndLine);
        Assert.AreEqual(generateBuilder.Locations[1].FilePath, node.ImplementationLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[1].StartLine, node.ImplementationLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[1].EndLine, node.ImplementationLocation!.EndLine);
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
        Assert.AreEqual(2, await dbContext.Cxx_DefineLocations.CountAsync());
        Assert.AreEqual(2, await dbContext.Cxx_ImplementationLocations.CountAsync());
        Node[] nodes = await dbContext.Cxx_Nodes
            .Include(item => item.Project)
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .ToArrayAsync();

        Assert.AreEqual(generateBuilder.Nodes[0].Id, nodes[0].Id);
        Assert.AreEqual(generateBuilder.Nodes[0].CursorKindSpelling, nodes[0].CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, nodes[0].Spelling);
        Assert.AreEqual(generateBuilder.Nodes[0].ProjectId, nodes[0].ProjectId);
        Assert.AreEqual(generateBuilder.Projects[0].Id, nodes[0].Project!.Id);
        Assert.AreEqual(generateBuilder.Projects[0].Name, nodes[0].Project!.Name);
        Assert.AreEqual(generateBuilder.Projects[0].CreateTime, nodes[0].Project!.CreateTime);
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, nodes[0].DefineLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, nodes[0].DefineLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, nodes[0].DefineLocation!.EndLine);
        Assert.AreEqual(generateBuilder.Locations[1].FilePath, nodes[0].ImplementationLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[1].StartLine, nodes[0].ImplementationLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[1].EndLine, nodes[0].ImplementationLocation!.EndLine);

        Assert.AreEqual(generateBuilder.Nodes[1].Id, nodes[1].Id);
        Assert.AreEqual(generateBuilder.Nodes[1].CursorKindSpelling, nodes[1].CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[1].Spelling, nodes[1].Spelling);
        Assert.AreEqual(generateBuilder.Nodes[1].ProjectId, nodes[1].ProjectId);
        Assert.AreEqual(generateBuilder.Projects[1].Id, nodes[1].Project!.Id);
        Assert.AreEqual(generateBuilder.Projects[1].Name, nodes[1].Project!.Name);
        Assert.AreEqual(generateBuilder.Projects[1].CreateTime, nodes[1].Project!.CreateTime);
        Assert.AreEqual(generateBuilder.Locations[2].FilePath, nodes[1].DefineLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[2].StartLine, nodes[1].DefineLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[2].EndLine, nodes[1].DefineLocation!.EndLine);
        Assert.AreEqual(generateBuilder.Locations[3].FilePath, nodes[1].ImplementationLocation!.FilePath);
        Assert.AreEqual(generateBuilder.Locations[3].StartLine, nodes[1].ImplementationLocation!.StartLine);
        Assert.AreEqual(generateBuilder.Locations[3].EndLine, nodes[1].ImplementationLocation!.EndLine);

        dbContext.Projects.Remove(nodes[1].Project!);
        await dbContext.SaveChangesAsync();
        Assert.AreEqual(1, await dbContext.Projects.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_DefineLocations.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_ImplementationLocations.CountAsync());
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
        Assert.AreEqual(2, await dbContext.Cxx_DefineLocations.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_ImplementationLocations.CountAsync());

        Node[] nodes = await dbContext.Cxx_Nodes
            .Include(item => item.Project)
            .Include(item => item.DefineLocation)
            .Include(item => item.ImplementationLocation)
            .ToArrayAsync();

        Node rootNode = nodes.First(item => item.Spelling == "RootNode1");

        dbContext.Cxx_DefineLocations.Remove(rootNode.DefineLocation!);
        dbContext.Cxx_ImplementationLocations.Remove(rootNode.ImplementationLocation!);
        await dbContext.SaveChangesAsync();

        Assert.AreEqual(2, await dbContext.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await dbContext.Cxx_DefineLocations.CountAsync());
        Assert.AreEqual(0, await dbContext.Cxx_ImplementationLocations.CountAsync());
    }
}
