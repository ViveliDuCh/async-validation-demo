using Microsoft.EntityFrameworkCore;
using SharedModels.EntityClasses;

namespace ModelFinalizingConventionDemo;

public class DemoDbContext : DbContext
{
    private readonly SharedModelsConvention _convention;

    public DbSet<UserRegistration> UserRegistrations { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Order> Orders { get; set; }

    public DemoDbContext(SharedModelsConvention convention)
    {
        _convention = convention;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=:memory:");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // SharedModels entities don't have [Key] attributes,
        // so we configure keys explicitly for EF Core
        modelBuilder.Entity<UserRegistration>(e =>
        {
            e.HasKey("Username");
            e.Property(x => x.Username).IsRequired();
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey("Title");
            e.Property(x => x.Title).IsRequired();
            e.Ignore(x => x.Delay);
        });

        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey("ProductName");
            e.Property(x => x.ProductName).IsRequired();
            e.Ignore(x => x.Delay);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Path B: IModelFinalizingConvention — one line, no service provider needed
        configurationBuilder.Conventions.Add(_ => _convention);
    }
}
