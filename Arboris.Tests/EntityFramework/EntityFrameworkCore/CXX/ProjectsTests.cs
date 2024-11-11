using Arboris.Tests.EntityFramework.CXX.TestData;

namespace Arboris.Tests.EntityFramework.EntityFrameworkCore.CXX;

[TestClass]
public class ProjectsTests
{
    private IDbContextFactory<ArborisDbContext> dbFactory = null!;
    private GenerateBuilder generateBuilder = null!;
    private ArborisDbContext db = null!;

    [TestInitialize]
    public async Task Initialize()
    {
        dbFactory = await DBContextInit.GetArborisDbContextFactoryAsync();
        db = await dbFactory.CreateDbContextAsync();
        generateBuilder = new(db);
    }

    [TestCleanup]
    public void Cleanup()
        => db.Dispose();

    [TestMethod]
    public async Task TestCreateProjectAsync()
    {
        await generateBuilder.GenerateProject1().BuildAsync();

        using ArborisDbContext dbContext = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(1, await dbContext.Projects.CountAsync());
        Project project = await dbContext.Projects.FirstAsync();
        Assert.AreEqual(generateBuilder.Projects[0].Id, project.Id);
        Assert.AreEqual(generateBuilder.Projects[0].CreateTime, project.CreateTime);
    }

    [TestMethod]
    public async Task TestDeleateProjectsAsync()
    {
        await generateBuilder.GenerateProject1().GenerateProject2().BuildAsync();

        ArborisDbContext dbContext = await dbFactory.CreateDbContextAsync();
        Assert.AreEqual(2, await dbContext.Projects.CountAsync());
        Assert.AreEqual(generateBuilder.Projects[0].CreateTime, (await dbContext.Projects.FindAsync(generateBuilder.Projects[0].Id))!.CreateTime);
        Assert.AreEqual(generateBuilder.Projects[1].CreateTime, (await dbContext.Projects.FindAsync(generateBuilder.Projects[1].Id))!.CreateTime);
        await dbContext.DisposeAsync();

        dbContext = await dbFactory.CreateDbContextAsync();
        dbContext.Projects.Remove(new() { Id = generateBuilder.Projects[0].Id });
        await dbContext.SaveChangesAsync();
        Assert.AreEqual(1, await dbContext.Projects.CountAsync());
        await dbContext.DisposeAsync();
    }
}
