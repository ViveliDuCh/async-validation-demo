using System.Reflection;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.EntityFrameworkCore.Metadata.Conventions.Infrastructure;

namespace PropertyAttributeConventionDemo;

/// <summary>
/// Path A: PropertyAttributeConventionBase&lt;UniqueUsernameAttribute&gt;
///
/// Typed to a specific attribute — mirrors how EF Core's built-in conventions
/// work (e.g., RequiredAttributeConvention, MaxLengthAttributeConvention).
///
/// When EF Core discovers [UniqueUsername] on a property, this convention
/// creates a UNIQUE INDEX — the same way [Required] creates NOT NULL.
///
/// Registration:
///   configurationBuilder.Conventions.Add(
///       sp => new UniqueUsernameConvention(
///           sp.GetRequiredService&lt;ProviderConventionSetBuilderDependencies&gt;()));
/// </summary>
public class UniqueUsernameConvention : PropertyAttributeConventionBase<UniqueUsernameAttribute>
{
    public List<string> Log { get; } = new();

    public UniqueUsernameConvention(ProviderConventionSetBuilderDependencies dependencies)
        : base(dependencies) { }

    protected override void ProcessPropertyAdded(
        IConventionPropertyBuilder propertyBuilder,
        UniqueUsernameAttribute attribute,
        MemberInfo clrMember,
        IConventionContext context)
    {
        var entityName = propertyBuilder.Metadata.DeclaringType.ClrType.Name;
        var propName = propertyBuilder.Metadata.Name;

        Log.Add($"  FOUND: {entityName}.{propName} → [{attribute.GetType().Name}]");

        // Create a UNIQUE INDEX — convention-defined schema action
        if (propertyBuilder.Metadata.DeclaringType is IConventionEntityType entityType)
        {
            entityType.Builder
                .HasIndex(new[] { propName }, fromDataAnnotation: true)
                ?.IsUnique(true, fromDataAnnotation: true);
        }

        Log.Add($"    → UNIQUE INDEX on {entityName}.{propName}");

        // Store a documentation annotation
        propertyBuilder.HasAnnotation(
            $"AsyncValidation:{attribute.GetType().Name}",
            $"Async validation via {attribute.GetType().FullName}");

        Log.Add($"    → Stored annotation: AsyncValidation:{attribute.GetType().Name}");
    }
}
