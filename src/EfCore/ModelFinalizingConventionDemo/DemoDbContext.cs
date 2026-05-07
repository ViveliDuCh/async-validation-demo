using Microsoft.EntityFrameworkCore;
using SharedModels.EntityClasses;

namespace ModelFinalizingConventionDemo;

public class DemoDbContext : DbContext
{
    private readonly SharedModelsConvention _convention;

    public DbSet<UserRegistration> UserRegistrations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Event> Events { get; set; }
    public DbSet<Order> Orders { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<MoneyTransfer> MoneyTransfers { get; set; }

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

        modelBuilder.Entity<User>(e =>
        {
            e.HasKey("Name");
            e.Property(x => x.Name).IsRequired();
            e.Ignore(x => x.Delay);
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

        modelBuilder.Entity<Profile>(e =>
        {
            e.HasKey("Username");
            e.Property(x => x.Username).IsRequired();
            e.Ignore(x => x.Delay);
        });

        modelBuilder.Entity<MoneyTransfer>(e =>
        {
            e.HasKey("FromAccount");
            e.Property(x => x.FromAccount).IsRequired();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Path B: IModelFinalizingConvention — one line, no service provider needed
        configurationBuilder.Conventions.Add(_ => _convention);
    }
}
