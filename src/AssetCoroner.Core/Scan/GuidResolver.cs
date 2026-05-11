namespace AssetCoroner.Core.Scan;

/// <summary>
/// Provides fast GUID-to-meta-file and asset-to-GUID lookups built from the repository's
/// .meta file index.
/// </summary>
public class GuidResolver
{
    // guid -> meta file path
    private readonly Dictionary<string, string> _guidToMeta;
    // asset path -> guid (from .meta files)
    private readonly Dictionary<string, string> _assetToGuid;

    /// <summary>
    /// Initialises a new <see cref="GuidResolver"/> from a pre-built index of asset paths and their GUIDs.
    /// </summary>
    /// <param name="metaIndex">Pairs of (assetPath, guid) extracted from all .meta files in the repository.</param>
    public GuidResolver(IEnumerable<(string assetPath, string guid)> metaIndex)
    {
        _guidToMeta = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _assetToGuid = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (assetPath, guid) in metaIndex)
        {
            _guidToMeta[guid] = assetPath + ".meta";
            _assetToGuid[assetPath] = guid;
        }
    }

    /// <summary>
    /// Attempts to resolve <paramref name="guid"/> to the path of its corresponding .meta file.
    /// Returns <c>true</c> and sets <paramref name="assetPath"/> when the GUID is known.
    /// </summary>
    public bool TryResolve(string guid, out string? assetPath)
    {
        if (_guidToMeta.TryGetValue(guid, out var meta))
        {
            assetPath = meta;
            return true;
        }
        assetPath = null;
        return false;
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="metaFilePath"/> has no corresponding asset file
    /// in <paramref name="allFilePaths"/>, indicating it is an orphaned .meta file.
    /// </summary>
    public bool IsOrphanedMeta(string metaFilePath, IEnumerable<string> allFilePaths)
    {
        var assetPath = metaFilePath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase)
            ? metaFilePath[..^5]
            : metaFilePath;

        return !allFilePaths.Contains(assetPath, StringComparer.OrdinalIgnoreCase);
    }
}
