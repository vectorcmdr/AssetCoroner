using AssetCoroner.Core.Models;

namespace AssetCoroner.Core;

public static class AssetClassifier
{
    private static readonly Dictionary<string, AssetCategory> ExtensionMap = new(StringComparer.OrdinalIgnoreCase)
    {
        // Mesh - binary-only formats
        { ".blend", AssetCategory.Mesh },
        { ".3ds",   AssetCategory.Mesh },
        { ".glb",   AssetCategory.Mesh },  // Binary glTF

        // Mesh - dual-format (binary or ASCII; binary assumed by default)
        { ".fbx",   AssetCategory.Mesh },  // ASCII variant starts with "; FBX" or "Kaydara FBX ASCII"
        { ".stl",   AssetCategory.Mesh },  // ASCII variant starts with "solid "

        // Mesh - text-only formats (XML/plaintext; not treated as binary)
        { ".obj",   AssetCategory.Mesh },  // Wavefront OBJ - always ASCII
        { ".dae",   AssetCategory.Mesh },  // Collada - always XML
        { ".gltf",  AssetCategory.Mesh },  // Text glTF - always JSON

        // Texture - always binary
        { ".png",   AssetCategory.Texture },
        { ".psd",   AssetCategory.Texture },
        { ".tga",   AssetCategory.Texture },
        { ".tiff",  AssetCategory.Texture },
        { ".tif",   AssetCategory.Texture },
        { ".exr",   AssetCategory.Texture },
        { ".jpg",   AssetCategory.Texture },
        { ".jpeg",  AssetCategory.Texture },
        { ".bmp",   AssetCategory.Texture },
        { ".gif",   AssetCategory.Texture },
        { ".hdr",   AssetCategory.Texture },  // Radiance HDR
        { ".raw",   AssetCategory.Texture },  // Raw image data

        // Audio - always binary
        { ".wav",   AssetCategory.Audio },
        { ".mp3",   AssetCategory.Audio },
        { ".ogg",   AssetCategory.Audio },
        { ".aiff",  AssetCategory.Audio },
        { ".aif",   AssetCategory.Audio },
        { ".flac",  AssetCategory.Audio },

        // Unity scene / prefab / asset files (typically YAML text; binary with "Force Binary" serialization)
        { ".unity",  AssetCategory.Scene },
        { ".prefab", AssetCategory.Prefab },
        { ".asset",  AssetCategory.Material },
        { ".mat",    AssetCategory.Material },

        // Material - text-only formats
        { ".mtl",    AssetCategory.Material },  // OBJ material library - always ASCII

        // Archive - always binary
        { ".zip",          AssetCategory.Archive },
        { ".unitypackage", AssetCategory.Archive },
        { ".tar",          AssetCategory.Archive },
        { ".gz",           AssetCategory.Archive },
        { ".7z",           AssetCategory.Archive },
        { ".rar",          AssetCategory.Archive },

        // Generic binary blobs
        { ".bin",   AssetCategory.Other },
        { ".dat",   AssetCategory.Other },
    };

    /// <summary>
    /// Extensions that are definitively always plain-text/XML and therefore
    /// human-readable and fully diff-able. They are still valid asset types
    /// (present in <see cref="ExtensionMap"/>) but must never be reported as
    /// binary by <see cref="IsBinaryAsset"/> when no explicit extension list
    /// is supplied.
    /// </summary>
    private static readonly HashSet<string> KnownTextExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".obj",   // Wavefront OBJ - always ASCII
        ".dae",   // Collada - always XML
        ".gltf",  // Text glTF - always JSON
        ".mtl",   // OBJ material library - always ASCII
    };

    /// <summary>
    /// Extensions whose on-disk format can be either binary or ASCII.
    /// Use <see cref="IsDualFormatExtension"/> to test membership safely;
    /// do not use LINQ Contains on this set directly as it bypasses
    /// the case-insensitive comparer.
    /// </summary>
    private static readonly HashSet<string> DualFormatExtensionSet = new(StringComparer.OrdinalIgnoreCase)
    {
        ".fbx",  // Binary: "Kaydara FBX Binary  \0"; ASCII: starts with "; FBX" or "Kaydara FBX ASCII"
        ".stl",  // ASCII: starts with "solid "; binary: 80-byte header
    };

    /// <summary>
    /// Extensions for Unity-specific serialized asset files. These files can be either
    /// YAML text (the default in modern Unity) or binary when a project has
    /// "Force Binary" serialization enabled. Use <see cref="UnityYamlParser.IsBinarySerialized"/>
    /// with file content to determine which format a specific file uses.
    /// </summary>
    private static readonly HashSet<string> UnitySerializedExtensionSet = new(StringComparer.OrdinalIgnoreCase)
    {
        ".unity",
        ".prefab",
        ".asset",
        ".mat",
    };

    /// <summary>
    /// Returns <c>true</c> when the extension of <paramref name="path"/> can
    /// represent either a binary or an ASCII/text file on disk. Callers who
    /// have file content available should use <see cref="LooksLikeBinaryFbx"/>
    /// or <see cref="LooksLikeBinaryStl"/> to distinguish the two variants.
    /// </summary>
    public static bool IsDualFormatExtension(string path) =>
        DualFormatExtensionSet.Contains(Path.GetExtension(path));

    /// <summary>
    /// Returns <c>true</c> when the file uses a Unity-specific serialized format
    /// that can be either YAML text or binary depending on the project's serialization mode.
    /// </summary>
    public static bool IsUnitySerializedFormat(string path) =>
        UnitySerializedExtensionSet.Contains(Path.GetExtension(path));

    /// <summary>
    /// Returns the <see cref="AssetCategory"/> for the file at <paramref name="path"/>
    /// based on its extension. Returns <see cref="AssetCategory.Other"/> for unrecognised extensions.
    /// </summary>
    public static AssetCategory Classify(string path)
    {
        var ext = Path.GetExtension(path);
        return ExtensionMap.TryGetValue(ext, out var cat) ? cat : AssetCategory.Other;
    }

    /// <summary>
    /// Returns <c>true</c> when the file should be treated as a binary (non-diffable) asset.
    /// <para>
    /// When <paramref name="extensions"/> is provided (e.g. from <c>ReviewConfig</c>) that
    /// explicit list governs the decision. When it is <c>null</c> the built-in
    /// <see cref="ExtensionMap"/> is used, but extensions listed in
    /// <see cref="KnownTextExtensions"/> are always excluded because they are
    /// definitively plain-text formats that can be diffed normally.
    /// </para>
    /// <para>
    /// For dual-format extensions (see <see cref="IsDualFormatExtension"/>), the extension
    /// alone is insufficient - use <see cref="LooksLikeBinaryFbx"/> or
    /// <see cref="LooksLikeBinaryStl"/> when file content is available.
    /// </para>
    /// </summary>
    public static bool IsBinaryAsset(string path, IEnumerable<string>? extensions = null)
    {
        var ext = Path.GetExtension(path);
        if (extensions != null)
            return extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
        return ExtensionMap.ContainsKey(ext) && !KnownTextExtensions.Contains(ext);
    }

    /// <summary>
    /// Inspects the leading text of an FBX file to determine whether it was saved in binary format.
    /// ASCII FBX starts with <c>; FBX</c> or <c>Kaydara FBX ASCII</c>;
    /// binary FBX starts with <c>Kaydara FBX Binary</c>.
    /// Returns <c>true</c> (binary assumed) when the content is too short to decide.
    /// </summary>
    public static bool LooksLikeBinaryFbx(string content)
    {
        if (string.IsNullOrEmpty(content)) return true;
        var trimmed = content.TrimStart();
        return !trimmed.StartsWith("; FBX", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("Kaydara FBX ASCII", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Inspects the leading text of an STL file to determine whether it was saved in binary format.
    /// ASCII STL starts with <c>solid </c>;
    /// binary STL has an 80-byte header with no guaranteed prefix.
    /// Returns <c>true</c> (binary assumed) when the content is too short to decide.
    /// </summary>
    public static bool LooksLikeBinaryStl(string content)
    {
        if (string.IsNullOrEmpty(content)) return true;
        return !content.TrimStart().StartsWith("solid ", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns an emoji icon representing the given <paramref name="category"/> for use in reports.
    /// </summary>
    public static string CategoryIcon(AssetCategory category) => category switch
    {
        AssetCategory.Mesh    => "🗿",
        AssetCategory.Texture => "🖼️",
        AssetCategory.Audio   => "🎵",
        AssetCategory.Scene   => "🎬",
        AssetCategory.Prefab  => "🧩",
        AssetCategory.Material=> "🎨",
        AssetCategory.Archive => "📦",
        _                     => "📁"
    };
}
