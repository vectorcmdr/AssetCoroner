using AssetCoroner.Core;
using AssetCoroner.Core.Models;

namespace AssetCoroner.Core.Tests;

public class AssetClassifierTests
{
    [Theory]
    [InlineData("model.fbx",                  AssetCategory.Mesh)]
    [InlineData("Assets/character.obj",       AssetCategory.Mesh)]
    [InlineData("mesh.dae",                   AssetCategory.Mesh)]
    [InlineData("mesh.gltf",                  AssetCategory.Mesh)]
    [InlineData("mesh.glb",                   AssetCategory.Mesh)]
    [InlineData("mesh.3ds",                   AssetCategory.Mesh)]
    [InlineData("mesh.stl",                   AssetCategory.Mesh)]
    [InlineData("texture.png",                AssetCategory.Texture)]
    [InlineData("Assets/Textures/albedo.psd", AssetCategory.Texture)]
    [InlineData("anim.gif",                   AssetCategory.Texture)]
    [InlineData("sky.hdr",                    AssetCategory.Texture)]
    [InlineData("heightmap.raw",              AssetCategory.Texture)]
    [InlineData("sound.wav",                  AssetCategory.Audio)]
    [InlineData("music.mp3",                  AssetCategory.Audio)]
    [InlineData("music.flac",                 AssetCategory.Audio)]
    [InlineData("sound.aif",                  AssetCategory.Audio)]
    [InlineData("Level01.unity",              AssetCategory.Scene)]
    [InlineData("Player.prefab",              AssetCategory.Prefab)]
    [InlineData("mat.mat",                    AssetCategory.Material)]
    [InlineData("mat.mtl",                    AssetCategory.Material)]
    [InlineData("game.unitypackage",          AssetCategory.Archive)]
    [InlineData("archive.rar",                AssetCategory.Archive)]
    [InlineData("archive.tar",                AssetCategory.Archive)]
    [InlineData("archive.gz",                 AssetCategory.Archive)]
    [InlineData("data.bin",                   AssetCategory.Other)]
    [InlineData("data.dat",                   AssetCategory.Other)]
    [InlineData("script.cs",                  AssetCategory.Other)]
    [InlineData("readme.md",                  AssetCategory.Other)]
    public void Classify_ReturnsCorrectCategory(string path, AssetCategory expected)
    {
        Assert.Equal(expected, AssetClassifier.Classify(path));
    }

    [Theory]
    // Definitely-binary formats
    [InlineData("model.fbx",   true)]
    [InlineData("model.blend", true)]
    [InlineData("model.glb",   true)]
    [InlineData("model.3ds",   true)]
    [InlineData("texture.png", true)]
    [InlineData("sky.hdr",     true)]
    [InlineData("sound.wav",   true)]
    [InlineData("music.flac",  true)]
    [InlineData("data.bin",    true)]
    [InlineData("data.rar",    true)]
    // Always-text formats - must NOT be classified as binary
    [InlineData("mesh.obj",    false)]
    [InlineData("mesh.dae",    false)]
    [InlineData("mesh.gltf",   false)]
    [InlineData("mat.mtl",     false)]
    // Unrelated files
    [InlineData("script.cs",   false)]
    [InlineData("readme.md",   false)]
    public void IsBinaryAsset_DefaultExtensions_CorrectResult(string path, bool expected)
    {
        Assert.Equal(expected, AssetClassifier.IsBinaryAsset(path));
    }

    [Fact]
    public void IsBinaryAsset_CustomExtensions_UsesCustomList()
    {
        var custom = new[] { ".fbx" };
        Assert.True(AssetClassifier.IsBinaryAsset("model.fbx", custom));
        Assert.False(AssetClassifier.IsBinaryAsset("texture.png", custom));
    }

    [Fact]
    public void Classify_IsCaseInsensitive()
    {
        Assert.Equal(AssetCategory.Mesh,    AssetClassifier.Classify("MODEL.FBX"));
        Assert.Equal(AssetCategory.Texture, AssetClassifier.Classify("TEXTURE.PNG"));
    }

    // Dual-format detection helpers

    // Note: "Kaydara FBX Binary  \x00..." contains a literal null byte (\x00) which is
    // part of the 20-byte binary FBX magic header ("Kaydara FBX Binary  " + 0x00).
    [Theory]
    [InlineData("; FBX 7.7.0 project file\nCreator: ...", false)]
    [InlineData("Kaydara FBX ASCII   \n...", false)]
    [InlineData("Kaydara FBX Binary  \x00...", true)]
    [InlineData("", true)]
    public void LooksLikeBinaryFbx_DetectsCorrectly(string content, bool expectedBinary)
    {
        Assert.Equal(expectedBinary, AssetClassifier.LooksLikeBinaryFbx(content));
    }

    [Theory]
    [InlineData("solid MyMesh\nfacet normal 0 0 1\n...", false)]
    [InlineData("  solid  WithLeadingSpace", false)]
    [InlineData("\x00\x00\x00binary header", true)]
    [InlineData("", true)]
    public void LooksLikeBinaryStl_DetectsCorrectly(string content, bool expectedBinary)
    {
        Assert.Equal(expectedBinary, AssetClassifier.LooksLikeBinaryStl(content));
    }

    [Fact]
    public void DualFormatExtensions_ContainsFbxAndStl()
    {
        Assert.True(AssetClassifier.IsDualFormatExtension("model.fbx"));
        Assert.True(AssetClassifier.IsDualFormatExtension("MODEL.FBX")); // case-insensitive
        Assert.True(AssetClassifier.IsDualFormatExtension("mesh.stl"));
        Assert.False(AssetClassifier.IsDualFormatExtension("texture.png"));
    }

    // Unity serialized format detection

    [Theory]
    [InlineData("Level01.unity",  true)]
    [InlineData("Player.prefab",  true)]
    [InlineData("MyMat.mat",      true)]
    [InlineData("Config.asset",   true)]
    [InlineData("LEVEL.UNITY",    true)]  // case-insensitive
    [InlineData("mesh.fbx",       false)]
    [InlineData("texture.png",    false)]
    [InlineData("script.cs",      false)]
    public void IsUnitySerializedFormat_DetectsCorrectly(string path, bool expected)
    {
        Assert.Equal(expected, AssetClassifier.IsUnitySerializedFormat(path));
    }
}
