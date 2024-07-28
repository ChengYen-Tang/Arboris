using Arboris.EntityFramework.EntityFrameworkCore;
using Arboris.Tests.EntityFramework.CXX.TestData;
using Microsoft.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.CXX;

[TestClass]
public class ProjectsTests
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
    public async Task TestCreateProjectAsync()
    {
        await generateBuilder.GenerateProject1().BuildAsync();

        Assert.AreEqual(1, await dbContext.Projects.CountAsync());
        Project project = await dbContext.Projects.FirstAsync();
        Assert.AreEqual(generateBuilder.Projects[0].Id, project.Id);
        Assert.AreEqual(generateBuilder.Projects[0].Name, project.Name);
        Assert.AreEqual(generateBuilder.Projects[0].CreateTime, project.CreateTime);
    }

    [TestMethod]
    public async Task TestDeleateProjectsAsync()
    {
        await generateBuilder.GenerateProject1().GenerateProject2().BuildAsync();

        Assert.AreEqual(2, await dbContext.Projects.CountAsync());
        Project project1 = await dbContext.Projects.FirstAsync();
        Assert.AreEqual(generateBuilder.Projects[0].Id, project1.Id);
        Assert.AreEqual(generateBuilder.Projects[0].Name, project1.Name);
        Assert.AreEqual(generateBuilder.Projects[0].CreateTime, project1.CreateTime);

        Project project2 = await dbContext.Projects.Skip(1).FirstAsync();
        Assert.AreEqual(generateBuilder.Projects[1].Id, project2.Id);
        Assert.AreEqual(generateBuilder.Projects[1].Name, project2.Name);
        Assert.AreEqual(generateBuilder.Projects[1].CreateTime, project2.CreateTime);

        dbContext.Projects.Remove(project1);
        await dbContext.SaveChangesAsync();
        Assert.AreEqual(1, await dbContext.Projects.CountAsync());
    }
}
