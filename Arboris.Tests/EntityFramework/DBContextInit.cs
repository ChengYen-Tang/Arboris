using Arboris.EntityFramework.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Arboris.Tests.EntityFramework;


public static class DBContextInit
{
    public static async Task<ArborisDbContext> GetArborisDbContextAsync()
    {
        DbContextOptions<ArborisDbContext> contextOptions = new DbContextOptionsBuilder<ArborisDbContext>()
            .UseInMemoryDatabase("ArborisTest")
            .ConfigureWarnings(b => b.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        ArborisDbContext dbContext = new(contextOptions);
        await dbContext.Database.EnsureDeletedAsync();
        await dbContext.Database.EnsureCreatedAsync();

        return dbContext;
    }
}
