using System.ComponentModel.DataAnnotations;

namespace Options.Shared;

/// <summary>
/// Options model whose async validation attribute provides a sync fallback,
/// so <c>.ValidateDataAnnotations() + .ValidateOnStart()</c> can drive
/// startup gating without crashing the host on the sync pass.
///
/// Bound from appsettings.json "CloudInfo" section.
/// </summary>
public class CloudInfoFallbackOptions
{
    /// <summary>Cloud storage provider name — must not be empty.</summary>
    [Required(ErrorMessage = "Storage provider is required.")]
    public string Storage { get; set; } = "";

    /// <summary>Cloud region — must not be empty.</summary>
    [Required(ErrorMessage = "Region is required.")]
    public string Region { get; set; } = "";

    /// <summary>
    /// Connection endpoint — sync fallback validates well-formedness;
    /// async path runs the full reachability probe.
    /// </summary>
    [Required(ErrorMessage = "Endpoint is required.")]
    [AsyncStorageExistsWithSyncFallback]
    public string Endpoint { get; set; } = "";
}
