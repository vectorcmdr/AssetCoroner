namespace AssetCoroner.Core.Models;

/// <summary>
/// Contains the results of a Unity GUID reference scan run against the changed files
/// in a pull request.
/// </summary>
public class ScanReport
{
    /// <summary>Gets or sets the list of broken or unresolvable GUID references found during the scan.</summary>
    public List<BrokenReference> BrokenReferences { get; set; } = new();
    /// <summary>Gets or sets paths to Unity asset files that could not be inspected because they use binary serialization.</summary>
    public List<string> UnInspectableFiles { get; set; } = new();
    /// <summary>Gets or sets the overall outcome of the scan.</summary>
    public ScanConclusion Conclusion { get; set; }
}

/// <summary>Describes the overall outcome of a Unity reference scan.</summary>
public enum ScanConclusion
{
    Success,
    Neutral,
    Failure
}
