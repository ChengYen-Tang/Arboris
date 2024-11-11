using Microsoft.Data.Sqlite;
using Moq;

namespace Arboris.Tests.EntityFramework.EntityFrameworkCore;


public static class DBContextInit
{
    private static SqliteConnection connection = null!;

    public static async Task<ArborisDbContext> GetArborisDbContextAsync()
    {
        await ConnectionAsync();
        ArborisDbContext dbContext = GenerateDbContextAsync();
        await DbInit(dbContext);

        return dbContext;
    }

    public static async Task<IDbContextFactory<ArborisDbContext>> GetArborisDbContextFactoryAsync()
    {
        await ConnectionAsync();
        var mockDbFactory = new Mock<IDbContextFactory<ArborisDbContext>>();
        mockDbFactory.Setup(factory => factory.CreateDbContext()).Returns(() => GenerateDbContextAsync());
        mockDbFactory.Setup(factory => factory.CreateDbContextAsync(default)).Returns(() => Task.FromResult(GenerateDbContextAsync()));
        IDbContextFactory<ArborisDbContext> dbFactor = mockDbFactory.Object;
        using ArborisDbContext db = await dbFactor.CreateDbContextAsync();
        await DbInit(db);

        return dbFactor;
    }

    private static ArborisDbContext GenerateDbContextAsync()
    {
        DbContextOptions<ArborisDbContext> contextOptions = new DbContextOptionsBuilder<ArborisDbContext>()
            .UseSqlite(connection)
            .Options;

        return new ArborisDbContext(contextOptions);
    }

    private static async Task ConnectionAsync()
    {
        if (connection is not null)
        {
            await connection.CloseAsync();
            await connection.DisposeAsync();
        }
        connection = new SqliteConnection("Data Source=:memory:");
    }

    private static async Task DbInit(ArborisDbContext dbContext)
    {
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();
    }
}
