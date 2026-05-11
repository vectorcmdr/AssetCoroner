using AssetCoroner.Core.Scan;

namespace AssetCoroner.Core.Tests;

public class UnityYamlParserTests
{
    private readonly UnityYamlParser _parser = new();

    private const string SamplePrefab = @"%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &1234567890
GameObject:
  m_Component:
  - component: {fileID: 100000, guid: abc123def456abc123def456abc123de, type: 3}
  - component: {fileID: 200000, guid: 00000000000000000000000000000000, type: 0}
--- !u!114 &9876543210
MonoBehaviour:
  m_Script: {fileID: 11500000, guid: deadbeefdeadbeefdeadbeefdeadbeef, type: 3}
  someRef: {fileID: 0, guid: 00000000000000000000000000000000, type: 0}";

    [Fact]
    public void ExtractGuids_FindsNonZeroGuids()
    {
        var guids = _parser.ExtractGuids(SamplePrefab).ToList();

        Assert.Contains(guids, g => g.guid == "abc123def456abc123def456abc123de");
        Assert.Contains(guids, g => g.guid == "deadbeefdeadbeefdeadbeefdeadbeef");
    }

    [Fact]
    public void ExtractGuids_ExcludesZeroGuids()
    {
        var guids = _parser.ExtractGuids(SamplePrefab).ToList();
        Assert.DoesNotContain(guids, g => g.guid == "00000000000000000000000000000000");
    }

    [Fact]
    public void ExtractGuids_ReturnsCorrectLineNumbers()
    {
        var guids = _parser.ExtractGuids(SamplePrefab).ToList();
        Assert.True(guids.All(g => g.lineNumber > 0));
    }

    [Fact]
    public void IsBinarySerialized_TextYaml_ReturnsFalse()
    {
        Assert.False(_parser.IsBinarySerialized(SamplePrefab));
    }

    [Fact]
    public void IsBinarySerialized_EmptyContent_ReturnsFalse()
    {
        Assert.False(_parser.IsBinarySerialized(string.Empty));
    }

    [Fact]
    public void IsBinarySerialized_NonYamlContent_ReturnsTrue()
    {
        Assert.True(_parser.IsBinarySerialized("\x00\x01\x02binary data"));
    }
}
