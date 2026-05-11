namespace AssetCoroner.Core.Models;

/// <summary>
/// Describes the size change for a single binary asset file between the base and head commits
/// of a pull request.
/// </summary>
public class BinaryDelta
{
    /// <summary>Gets or sets the repository-relative path to the asset file.</summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>Gets or sets the broad category of the asset.</summary>
    public AssetCategory Category { get; set; }
    /// <summary>Gets or sets whether the file was added, replaced, or deleted in this pull request.</summary>
    public DeltaChangeType ChangeType { get; set; }
    /// <summary>Gets or sets the size of the asset in the base commit, or <c>null</c> for newly added files.</summary>
    public long? PreviousSizeBytes { get; set; }
    /// <summary>Gets or sets the size of the asset in the head commit, or <c>null</c> for deleted files.</summary>
    public long? NewSizeBytes { get; set; }

    /// <summary>
    /// Gets the signed byte difference between the new and previous sizes.
    /// Treats missing values as zero.
    /// </summary>
    public long DeltaBytes =>
        (NewSizeBytes ?? 0) - (PreviousSizeBytes ?? 0);

    /// <summary>
    /// Gets the size change as a percentage of the previous size.
    /// Returns 100 when there is no previous size (i.e. the file is new).
    /// </summary>
    public double DeltaPercent =>
        PreviousSizeBytes is > 0
            ? (double)DeltaBytes / PreviousSizeBytes.Value * 100.0
            : 100.0;

    /// <summary>Gets or sets a value indicating whether the file is stored as a Git LFS pointer.</summary>
    public bool IsLfsPointer { get; set; }

    /// <summary>
    /// For Unity-specific asset files (.unity, .prefab, .asset, .mat):
    /// true = binary serialized, false = text/YAML serialized, null = not a Unity file.
    /// </summary>
    public bool? IsUnityBinarySerialized { get; set; }
}

/// <summary>Describes how a binary asset changed within a pull request.</summary>
public enum DeltaChangeType
{
    New,
    Replacement,
    Deletion
}
