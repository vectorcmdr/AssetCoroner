namespace AssetCoroner.Core.Models;

/// <summary>
/// Represents a single broken or suspicious GUID reference found while scanning a Unity asset file.
/// </summary>
public class BrokenReference
{
    /// <summary>Gets or sets the repository-relative path to the file containing the broken reference.</summary>
    public string FilePath { get; set; } = string.Empty;
    /// <summary>Gets or sets the 1-based line number in the file where the reference was found.</summary>
    public int LineNumber { get; set; }
    /// <summary>Gets or sets the Unity GUID string that could not be resolved.</summary>
    public string Guid { get; set; } = string.Empty;
    /// <summary>Gets or sets the category of broken reference detected.</summary>
    public BrokenReferenceKind Kind { get; set; }
    /// <summary>Gets or sets an optional human-readable description providing additional context.</summary>
    public string? Details { get; set; }
}

/// <summary>Classifies the type of broken Unity asset reference.</summary>
public enum BrokenReferenceKind
{
    MissingGuid,
    OrphanedMeta,
    BrokenScriptReference,
    DanglingPrefabVariant
}
