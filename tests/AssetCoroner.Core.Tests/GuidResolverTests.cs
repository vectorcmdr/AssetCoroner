using AssetCoroner.Core.Scan;

namespace AssetCoroner.Core.Tests;

public class GuidResolverTests
{
    private readonly GuidResolver _resolver;

    public GuidResolverTests()
    {
        var index = new[]
        {
            ("Assets/Models/hero.fbx", "abc123def456abc123def456abc123de"),
            ("Assets/Scripts/Player.cs", "deadbeefdeadbeefdeadbeefdeadbeef")
        };
        _resolver = new GuidResolver(index);
    }

    [Fact]
    public void TryResolve_KnownGuid_ReturnsTrue()
    {
        Assert.True(_resolver.TryResolve("abc123def456abc123def456abc123de", out var path));
        Assert.Equal("Assets/Models/hero.fbx.meta", path);
    }

    [Fact]
    public void TryResolve_UnknownGuid_ReturnsFalse()
    {
        Assert.False(_resolver.TryResolve("ffffffffffffffffffffffffffffffff", out var path));
        Assert.Null(path);
    }

    [Fact]
    public void TryResolve_IsCaseInsensitive()
    {
        Assert.True(_resolver.TryResolve("ABC123DEF456ABC123DEF456ABC123DE", out _));
    }

    [Fact]
    public void IsOrphanedMeta_FileExists_ReturnsFalse()
    {
        var allFiles = new[] { "Assets/Models/hero.fbx", "Assets/Models/hero.fbx.meta" };
        Assert.False(_resolver.IsOrphanedMeta("Assets/Models/hero.fbx.meta", allFiles));
    }

    [Fact]
    public void IsOrphanedMeta_FileDeleted_ReturnsTrue()
    {
        var allFiles = new[] { "Assets/Models/hero.fbx.meta" }; // source asset missing
        Assert.True(_resolver.IsOrphanedMeta("Assets/Models/hero.fbx.meta", allFiles));
    }
}
