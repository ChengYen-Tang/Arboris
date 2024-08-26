using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.EntityFramework.EntityFrameworkCore.CXX;
using Arboris.EntityFramework.Repositories;
using Arboris.Models.Analyze.CXX;
using Arboris.Tests.EntityFramework.EntityFrameworkCore;
using Arboris.Tests.EntityFramework.Repositories.TestData;
using FluentResults;
using Microsoft.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.Repositories;

[TestClass]
public class CxxRepositoryTests
{
    private IDbContextFactory<ArborisDbContext> dbFactory = null!;
    private GenerateBuilder generateBuilder = null!;
    private CxxRepository cxxRepository = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        dbFactory = await DBContextInit.GetArborisDbContextFactoryAsync();
        generateBuilder = new(await dbFactory.CreateDbContextAsync());
        cxxRepository = new(dbFactory);
    }

    [TestMethod]
    public async Task TestAddNodeAsync()
    {
        await generateBuilder.GenerateProject1().BuildAsync();

        AddNode addNode = new(generateBuilder.Projects[0].Id, "Class", "RootNode1", null, new("RootNode1.h", 1, 1), null);
        await cxxRepository.AddNodeAsync(addNode);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(1, await db.Cxx_Nodes.CountAsync());
        Assert.AreEqual(1, await db.Cxx_DefineLocations.CountAsync());
        Node node = await db.Cxx_Nodes.Include(item => item.DefineLocation).Include(item => item.ImplementationLocation).FirstAsync();
        Assert.AreEqual(addNode.CursorKindSpelling, node.CursorKindSpelling);
        Assert.AreEqual(addNode.Spelling, node.Spelling);
        Assert.IsNull(node.ImplementationLocation);
        Assert.AreEqual(addNode.DefineLocation.FilePath, node.DefineLocation.FilePath);
        Assert.AreEqual(addNode.DefineLocation.StartLine, node.DefineLocation.StartLine);
        Assert.AreEqual(addNode.DefineLocation.EndLine, node.DefineLocation.EndLine);
    }

    [TestMethod]
    public async Task TestGetNodeFromDefineLocation()
    {
        await generateBuilder.GenerateProject1().GenerateRootNode1().BuildAsync();

        Models.Analyze.CXX.Location location = new(generateBuilder.Locations[0].FilePath, generateBuilder.Locations[0].StartLine, generateBuilder.Locations[0].EndLine);
        Result<Models.Analyze.CXX.Node> result = await cxxRepository.GetNodeFromDefineLocation(location);

        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(generateBuilder.Nodes[0].CursorKindSpelling, result.Value.CursorKindSpelling);
        Assert.AreEqual(generateBuilder.Nodes[0].Spelling, result.Value.Spelling);
        Assert.AreEqual(generateBuilder.Nodes[0].CxType, result.Value.CxType);
        Assert.AreEqual(generateBuilder.Locations[0].FilePath, result.Value.DefineLocation.FilePath);
        Assert.AreEqual(generateBuilder.Locations[0].StartLine, result.Value.DefineLocation.StartLine);
        Assert.AreEqual(generateBuilder.Locations[0].EndLine, result.Value.DefineLocation.EndLine);
    }

    [TestMethod]
    public async Task TestGetNodeFromDefineLocation_LocationNotFound()
    {
        Result<Models.Analyze.CXX.Node> result = await cxxRepository.GetNodeFromDefineLocation(new("NotFound.h", 1, 1));

        Assert.IsTrue(result.IsFailed);
        Assert.AreEqual("Location not found", result.Errors[0].Message);
    }

    [TestMethod]
    public async Task TestUpdateNodeAsync()
    {
        await generateBuilder.GenerateProject1().GenerateRootNode1().BuildAsync();

        Models.Analyze.CXX.Node node = new()
        {
            Id = generateBuilder.Nodes[0].Id,
            CursorKindSpelling = "Struct",
            Spelling = "RootNode",
            CxType = "Struct",
            DefineLocation = new("RootNode.h", 0, 0),
            ImplementationLocation = new("RootNode.cpp", 2, 2)
        };
        await cxxRepository.UpdateNodeAsync(node);

        using ArborisDbContext db = await dbFactory.CreateDbContextAsync();
        Node updatedNode = await db.Cxx_Nodes.Include(item => item.DefineLocation).Include(item => item.ImplementationLocation).FirstAsync();
        Assert.AreEqual("Struct", updatedNode.CursorKindSpelling);
        Assert.AreEqual("RootNode", updatedNode.Spelling);
        Assert.AreEqual("Struct", updatedNode.CxType);
        Assert.AreEqual("RootNode.h", updatedNode.DefineLocation.FilePath);
        Assert.AreEqual(0u, updatedNode.DefineLocation.StartLine);
        Assert.AreEqual(0u, updatedNode.DefineLocation.EndLine);
        Assert.AreEqual("RootNode.cpp", updatedNode.ImplementationLocation!.FilePath);
        Assert.AreEqual(2u, updatedNode.ImplementationLocation!.StartLine);
        Assert.AreEqual(2u, updatedNode.ImplementationLocation!.EndLine);
    }
}
