using System.ComponentModel.DataAnnotations;

namespace Options.Shared;

/// <summary>
/// Simple options POCO bound from appsettings.json "CloudInfo" section.
/// Uses both sync and async validation attributes to prove
/// the bypass pipeline handles both phases.
/// </summary>
public class CloudInfoOptions
{
    /// <summary>Cloud storage provider name — must not be empty.</summary>
    [Required(ErrorMessage = "Storage provider is required.")]
    public string Storage { get; set; } = "";

    /// <summary>Cloud region — must not be empty.</summary>
    [Required(ErrorMessage = "Region is required.")]
    public string Region { get; set; } = "";

    /// <summary>
    /// Connection endpoint — validated asynchronously
    /// (simulates checking that the endpoint is reachable).
    /// </summary>
    [Required(ErrorMessage = "Endpoint is required.")]
    [AsyncStorageExists]
    public string Endpoint { get; set; } = "";
}
