using System.Text.RegularExpressions;

namespace AssetCoroner.Core.Scan;

/// <summary>
/// Parses Unity YAML asset files to extract GUID references and detect binary serialization.
/// </summary>
public class UnityYamlParser
{
    // Matches: guid: <32 hex chars>
    private static readonly Regex GuidPattern = new(
        @"guid:\s*([0-9a-fA-F]{32})",
        RegexOptions.Compiled | RegexOptions.Multiline);

    // Matches fileID in object references: {fileID: 123, guid: abc, type: 3}
    private static readonly Regex FileIdGuidPattern = new(
        @"\{fileID:\s*(?<fileID>-?\d+),\s*guid:\s*(?<guid>[0-9a-fA-F]{32}),\s*type:\s*\d+\}",
        RegexOptions.Compiled | RegexOptions.Multiline);

    /// <summary>
    /// Extracts all non-null GUIDs from <paramref name="content"/>, returning each
    /// with its 1-based line number. Zeroed GUIDs (32 zeros) are skipped because they
    /// represent null or unset references.
    /// </summary>
    /// <param name="content">Decoded text content of a Unity YAML asset file.</param>
    public IEnumerable<(int lineNumber, string guid)> ExtractGuids(string content)
    {
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var matches = GuidPattern.Matches(lines[i]);
            foreach (Match m in matches)
            {
                var guid = m.Groups[1].Value.ToLowerInvariant();
                // Skip zeroed GUIDs (null references)
                if (guid != "00000000000000000000000000000000")
                    yield return (i + 1, guid);
            }
        }
    }

    /// <summary>
    /// Extracts all inline object references of the form
    /// <c>{fileID: N, guid: X, type: Y}</c> from <paramref name="content"/>,
    /// returning each with its 1-based line number, fileID, and GUID.
    /// Zeroed GUIDs are skipped.
    /// </summary>
    /// <param name="content">Decoded text content of a Unity YAML asset file.</param>
    public IEnumerable<(int lineNumber, string fileId, string guid)> ExtractObjectReferences(string content)
    {
        var lines = content.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            var matches = FileIdGuidPattern.Matches(lines[i]);
            foreach (Match m in matches)
            {
                var guid = m.Groups["guid"].Value.ToLowerInvariant();
                if (guid != "00000000000000000000000000000000")
                    yield return (i + 1, m.Groups["fileID"].Value, guid);
            }
        }
    }

    /// <summary>
    /// Returns <c>true</c> when <paramref name="content"/> appears to be a Unity binary-serialized
    /// asset rather than a YAML text file. Text files begin with <c>%YAML</c>, <c>---</c>, or <c>%TAG</c>;
    /// all other content is treated as binary. Returns <c>false</c> for null or empty input.
    /// </summary>
    /// <param name="content">Decoded content of a Unity asset file.</param>
    public bool IsBinarySerialized(string content)
    {
        // Unity binary files start with specific magic bytes; 
        // text YAML starts with %YAML or ---
        if (string.IsNullOrEmpty(content)) return false;
        return !content.StartsWith("%YAML") && !content.StartsWith("---") && !content.StartsWith("%TAG");
    }
}
