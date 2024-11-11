namespace Arboris.Tests.EntityFramework.CXX.TestData;

public partial class GenerateBuilder(ArborisDbContext db)
{
    public async Task BuildAsync()
        => await db.SaveChangesAsync();
}
