using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MsSqlFactory;

public class DesignTimeMyDbContextFactory : IDesignTimeDbContextFactory<MyDbContext>
{
    public MyDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();
        optionsBuilder.UseSqlServer(
            "Server=localhost,1487;Database=InventoryServiceDb;User Id=sa;Password=YourStrong!Passw0rd;Encrypt=False;TrustServerCertificate=True;");
        return new MyDbContext(optionsBuilder.Options);
    }
}