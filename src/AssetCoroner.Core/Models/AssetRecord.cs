namespace AssetCoroner.Core.Models;

/// <summary>
/// Represents a single binary asset discovered during a repository audit or pull-request review.
/// </summary>
public class AssetRecord
{
    /// <summary>Gets or sets the repository-relative path to the asset file.</summary>
    public string Path { get; set; } = string.Empty;
    /// <summary>Gets or sets the file extension including the leading dot (e.g. ".png").</summary>
    public string Extension { get; set; } = string.Empty;
    /// <summary>Gets or sets the size of the asset in bytes.</summary>
    public long SizeBytes { get; set; }
    /// <summary>Gets or sets a value indicating whether this asset is tracked by Git LFS.</summary>
    public bool IsLfsTracked { get; set; }
    /// <summary>Gets or sets the broad category the asset belongs to.</summary>
    public AssetCategory Category { get; set; }
    /// <summary>Gets or sets the number of commits in which this asset was modified.</summary>
    public int CommitChurnCount { get; set; }
    /// <summary>Gets or sets the Git blob SHA for this asset at the audited commit.</summary>
    public string Sha { get; set; } = string.Empty;
}

/// <summary>Broad classification of a binary asset by its file type.</summary>
public enum AssetCategory
{
    Mesh,
    Texture,
    Audio,
    Scene,
    Prefab,
    Archive,
    Material,
    Other
}
