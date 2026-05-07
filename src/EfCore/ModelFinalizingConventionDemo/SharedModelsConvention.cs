using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using SharedModels.ValidationClasses;

namespace ModelFinalizingConventionDemo;

/// <summary>
/// Path B: IModelFinalizingConvention
///
/// Scans ALL properties in ALL entity types for AsyncValidationAttribute
/// subclasses and applies convention-defined schema actions.
///
/// Registration (one line, no service provider deps):
///   configurationBuilder.Conventions.Add(_ => new SharedModelsConvention());
/// </summary>
public class SharedModelsConvention : IModelFinalizingConvention
{
    public List<string> Log { get; } = new();

    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            // Property-level async attributes
            foreach (var property in entityType.GetDeclaredProperties())
            {
                var member = property.PropertyInfo as MemberInfo ?? property.FieldInfo;
                if (member is null) continue;

                var asyncAttrs = member
                    .GetCustomAttributes(typeof(AsyncValidationAttribute), inherit: true)
                    .Cast<AsyncValidationAttribute>()
                    .ToArray();

                foreach (var attr in asyncAttrs)
                {
                    var attrName = attr.GetType().Name;
                    var entity = entityType.ClrType.Name;
                    var prop = property.Name;

                    Log.Add($"  FOUND: {entity}.{prop} → [{attrName}]");

                    // Convention-defined schema actions per attribute type
                    switch (attr)
                    {
                        case UniqueUsernameAttribute:
                            entityType.Builder
                                .HasIndex(new[] { prop }, fromDataAnnotation: true)
                                ?.IsUnique(true, fromDataAnnotation: true);
                            Log.Add($"    → UNIQUE INDEX on {entity}.{prop}");
                            break;

                        case UniqueEmailAttribute:
                            entityType.Builder
                                .HasIndex(new[] { prop }, fromDataAnnotation: true)
                                ?.IsUnique(true, fromDataAnnotation: true);
                            Log.Add($"    → UNIQUE INDEX on {entity}.{prop}");
                            break;

                        case AsyncOnlyEmailDomainAttribute:
                            Log.Add($"    → No schema action (domain validation only)");
                            break;

                        case IsValidNameAttribute:
                            Log.Add($"    → No schema action (name validation only)");
                            break;

                        default:
                            Log.Add($"    → No schema action (unknown async attr)");
                            break;
                    }

                    property.Builder.HasAnnotation(
                        $"AsyncValidation:{attrName}",
                        attr.GetType().FullName);
                }
            }

            // Class-level async attributes
            var classAttrs = entityType.ClrType
                .GetCustomAttributes(typeof(AsyncValidationAttribute), inherit: true)
                .Cast<AsyncValidationAttribute>()
                .ToArray();

            foreach (var attr in classAttrs)
            {
                var attrName = attr.GetType().Name;
                var entity = entityType.ClrType.Name;

                Log.Add($"  FOUND: {entity} (class-level) → [{attrName}]");

                entityType.Builder.HasAnnotation(
                    $"AsyncValidation:{attrName}",
                    attr.GetType().FullName);
                Log.Add($"    → Stored entity annotation");
            }
        }
    }
}
