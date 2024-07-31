using Arboris.EntityFramework.EntityFrameworkCore;

namespace Arboris.Tests.EntityFramework.Repositories.TestData;

public partial class GenerateBuilder(ArborisDbContext db)
{
    public async Task BuildAsync()
    {
        await db.SaveChangesAsync();
        await db.DisposeAsync();
    }
}
