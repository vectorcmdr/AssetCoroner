namespace AssetCoroner.Core.Models;

/// <summary>
/// Contains the results of a repository binary-asset audit, including threshold violations
/// and LFS recommendations.
/// </summary>
public class AuditReport
{
    /// <summary>Gets or sets every binary asset found in the repository tree.</summary>
    public List<AssetRecord> AllBinaryAssets { get; set; } = new();
    /// <summary>Gets or sets assets whose size meets or exceeds the configured warn threshold.</summary>
    public List<AssetRecord> OverThreshold { get; set; } = new();
    /// <summary>Gets or sets assets that exceed the LFS recommendation threshold and are not yet LFS-tracked.</summary>
    public List<AssetRecord> LfsRecommended { get; set; } = new();
    /// <summary>Gets or sets the ten largest binary assets, ordered by descending size.</summary>
    public List<AssetRecord> TopLargestAssets { get; set; } = new();
    /// <summary>Gets or sets the combined size of all binary assets, in bytes.</summary>
    public long TotalBinarySizeBytes { get; set; }
    /// <summary>Gets or sets the total size of all blobs in the repository tree, in bytes.</summary>
    public long TotalRepoSizeBytes { get; set; }
    /// <summary>
    /// Gets the percentage of the total repository size that is made up of binary assets.
    /// Returns 0 when <see cref="TotalRepoSizeBytes"/> is zero.
    /// </summary>
    public double BinaryAssetPercent =>
        TotalRepoSizeBytes > 0
            ? (double)TotalBinarySizeBytes / TotalRepoSizeBytes * 100.0
            : 0;
    /// <summary>Gets or sets the overall outcome of the audit.</summary>
    public AuditConclusion Conclusion { get; set; }
}

/// <summary>Describes the overall outcome of a repository audit.</summary>
public enum AuditConclusion
{
    Pass,
    Warning,
    Failure
}
