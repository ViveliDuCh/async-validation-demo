using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

/// <summary>
/// Copy of AsyncRegistrationScreenAttribute that adds ISchemaDescriptor.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class AsyncRegistrationScreenDescriptorAttribute : AsyncValidationAttribute, ISchemaDescriptor
{
    public string? SchemaDescription => "Registration requires async server-side screening.";
    public string? SchemaFormat => null;
    public string? SchemaPattern => null;
    public int? SchemaMaxLength => null;
    public int? SchemaMinLength => null;
    public IReadOnlyDictionary<string, object>? SchemaExtensions =>
        new Dictionary<string, object>
        {
            ["x-async-validation"] = true,
            ["x-requires-server-check"] = true,
            ["x-async-class-validation-type"] = "registration-screen"
        };

    protected override async ValueTask<ValidationResult?> IsValidAsync(
        object? value,
        ValidationContext validationContext,
        CancellationToken cancellationToken)
    {
        var type = validationContext.ObjectType;
        var instance = validationContext.ObjectInstance;
        var email = type.GetProperty("Email")?.GetValue(instance) as string;

        await Task.Delay(50, cancellationToken);

        if (email is not null &&
            email.EndsWith("@blocked.com", StringComparison.OrdinalIgnoreCase))
        {
            return new ValidationResult(
                "Registration blocked: email domain is not allowed.",
                new[] { "Email" });
        }

        return ValidationResult.Success;
    }
}
