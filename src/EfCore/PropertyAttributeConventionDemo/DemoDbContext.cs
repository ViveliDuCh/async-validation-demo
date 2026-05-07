using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace PropertyAttributeConventionDemo;

public class DemoDbContext : DbContext
{
    private readonly UniqueUsernameConvention _convention;

    public DbSet<UserRegistration> Users { get; set; }

    public DemoDbContext(UniqueUsernameConvention convention)
    {
        _convention = convention;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=:memory:");

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Path A: PropertyAttributeConventionBase<T>
        // Requires resolving ProviderConventionSetBuilderDependencies from the
        // internal service provider — mirrors the built-in convention architecture.
        configurationBuilder.Conventions.Add(
            sp => new UniqueUsernameConvention(
                sp.GetRequiredService<ProviderConventionSetBuilderDependencies>()));

        // Also register our shared instance so we can read the log
        configurationBuilder.Conventions.Add(_ => _convention);
    }
}
