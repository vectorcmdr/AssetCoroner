namespace AssetCoroner.Core.Config;

/// <summary>
/// Root configuration object for AssetCoroner, combining settings for each analysis engine.
/// </summary>
public class AssetCoronerConfig
{
    /// <summary>Gets or sets the configuration for the repository audit engine.</summary>
    public AuditConfig Audit { get; set; } = new();
    /// <summary>Gets or sets the configuration for the Unity reference scan engine.</summary>
    public ScanConfig Scan { get; set; } = new();
    /// <summary>Gets or sets the configuration for the pull-request review engine.</summary>
    public ReviewConfig Review { get; set; } = new();

    /// <summary>Gets a new <see cref="AssetCoronerConfig"/> populated with default values.</summary>
    public static AssetCoronerConfig Default => new();
}

/// <summary>
/// Configuration for the scheduled repository audit that checks binary asset sizes.
/// </summary>
public class AuditConfig
{
    /// <summary>Gets or sets a value indicating whether the audit engine runs on schedule.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Gets or sets the cron expression that controls when the audit runs.</summary>
    public string Schedule { get; set; } = "0 3 * * 1";
    /// <summary>Gets or sets the file size in megabytes above which a warning is issued.</summary>
    public int WarnThresholdMb { get; set; } = 5;
    /// <summary>Gets or sets the file size in megabytes above which the audit fails.</summary>
    public int CriticalThresholdMb { get; set; } = 25;
    /// <summary>Gets or sets the file size in megabytes above which Git LFS usage is recommended.</summary>
    public int LfsRecommendThresholdMb { get; set; } = 10;
    /// <summary>Gets or sets a value indicating whether audit results are posted as a commit status.</summary>
    public bool ReportCommit { get; set; } = false;
}

/// <summary>
/// Configuration for the Unity GUID reference scan that runs on pull requests.
/// </summary>
public class ScanConfig
{
    /// <summary>Gets or sets a value indicating whether the scan engine is active.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Gets or sets a value indicating whether broken GUID references cause a workflow failure.</summary>
    public bool FailOnBrokenRefs { get; set; } = true;
    /// <summary>Gets or sets a value indicating whether orphaned .meta files generate warnings.</summary>
    public bool WarnOnOrphanedMeta { get; set; } = true;
}

/// <summary>
/// Configuration for the pull-request review engine that tracks binary asset size changes.
/// </summary>
public class ReviewConfig
{
    /// <summary>Gets or sets a value indicating whether the review engine is active.</summary>
    public bool Enabled { get; set; } = true;
    /// <summary>Gets or sets a value indicating whether a summary comment is posted to the pull request.</summary>
    public bool PostComment { get; set; } = true;
    /// <summary>Gets or sets a value indicating whether the workflow fails when a critical-size file is detected.</summary>
    public bool BlockOnCritical { get; set; } = false;
    /// <summary>Gets or sets the size delta in megabytes above which a file is considered critical.</summary>
    public int CriticalDeltaMb { get; set; } = 50;
    /// <summary>Gets or sets the list of file extensions tracked by the review engine.</summary>
    public List<string> Extensions { get; set; } = new()
    {
        // Mesh - binary-only
        ".blend", ".3ds", ".glb",
        // Mesh - dual-format (binary or ASCII; binary assumed by default - use AssetClassifier helpers to distinguish)
        ".fbx", ".stl",
        // Mesh - text-only (tracked for size even though they are plain-text/XML)
        ".obj",
        // Material
        ".mat", ".mtl", ".asset",
        // Scene / Prefab
        ".prefab", ".unity",
        // Audio - always binary
        ".wav", ".mp3", ".ogg", ".aiff", ".aif", ".flac",
        // Texture - always binary
        ".png", ".psd", ".tga", ".tiff", ".tif", ".exr",
        ".jpg", ".jpeg", ".bmp", ".gif", ".hdr", ".raw",
        // Archive - always binary
        ".zip", ".unitypackage", ".tar", ".gz", ".7z", ".rar",
        // Generic binary blobs
        ".bin", ".dat",
    };
}
