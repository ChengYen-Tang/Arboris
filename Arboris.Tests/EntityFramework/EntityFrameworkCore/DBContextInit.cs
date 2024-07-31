using Arboris.EntityFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace Arboris.Tests.EntityFramework.EntityFrameworkCore;


public static class DBContextInit
{
    public static async Task<ArborisDbContext> GetArborisDbContextAsync()
    {
        ArborisDbContext dbContext = GenerateDbContextAsync();
        await DbInit(dbContext);

        return dbContext;
    }

    public static async Task<IDbContextFactory<ArborisDbContext>> GetArborisDbContextFactoryAsync()
    {
        var mockDbFactory = new Mock<IDbContextFactory<ArborisDbContext>>();
        mockDbFactory.Setup(factory => factory.CreateDbContext()).Returns(() => GenerateDbContextAsync());
        mockDbFactory.Setup(factory => factory.CreateDbContextAsync(default)).Returns(() => Task.FromResult(GenerateDbContextAsync()));
        IDbContextFactory<ArborisDbContext> dbFactor = mockDbFactory.Object;
        await DbInit(await dbFactor.CreateDbContextAsync());

        return dbFactor;
    }

    private static ArborisDbContext GenerateDbContextAsync()
    {
        DbContextOptions<ArborisDbContext> contextOptions = new DbContextOptionsBuilder<ArborisDbContext>()
            .UseInMemoryDatabase("ArborisTest")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new(contextOptions);
    }

    private static async Task DbInit(ArborisDbContext dbContext)
    {
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
