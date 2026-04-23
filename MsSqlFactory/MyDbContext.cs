using Microsoft.EntityFrameworkCore;

namespace MsSqlFactory;

public class MyDbContext : DbContext
{
    public DbSet<UuidTable> UuidTable { get; private set; }

    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UuidTable>(entity =>
        {
            entity.HasKey(x => x.Uuid);
            entity.Property(x => x.Uuid).HasColumnName("uuid");
            entity.Property(x => x.Order).HasColumnName("order");
            entity.ToTable("uuids");
        });
    }
}