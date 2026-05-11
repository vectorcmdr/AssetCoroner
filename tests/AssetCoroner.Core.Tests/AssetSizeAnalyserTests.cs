using AssetCoroner.Core.Audit;
using AssetCoroner.Core.Config;
using AssetCoroner.Core.Models;

namespace AssetCoroner.Core.Tests;

public class AssetSizeAnalyserTests
{
    private readonly AuditConfig _defaultConfig = new()
    {
        WarnThresholdMb = 5,
        CriticalThresholdMb = 25,
        LfsRecommendThresholdMb = 10
    };

    [Fact]
    public void Analyse_EmptyAssets_ReturnsPassWithZeroTotals()
    {
        var analyser = new AssetSizeAnalyser(_defaultConfig);
        var report = analyser.Analyse(Enumerable.Empty<AssetRecord>(), 0);

        Assert.Equal(AuditConclusion.Pass, report.Conclusion);
        Assert.Empty(report.AllBinaryAssets);
        Assert.Empty(report.OverThreshold);
        Assert.Equal(0, report.TotalBinarySizeBytes);
    }

    [Fact]
    public void Analyse_SmallAssets_ReturnsPass()
    {
        var analyser = new AssetSizeAnalyser(_defaultConfig);
        var assets = new[]
        {
            new AssetRecord { Path = "small.fbx", SizeBytes = 1024 * 1024, Category = AssetCategory.Mesh } // 1 MB
        };
        var report = analyser.Analyse(assets, 10 * 1024 * 1024);

        Assert.Equal(AuditConclusion.Pass, report.Conclusion);
        Assert.Empty(report.OverThreshold);
    }

    [Fact]
    public void Analyse_FilesAboveWarnThreshold_ReturnsWarning()
    {
        var analyser = new AssetSizeAnalyser(_defaultConfig);
        var assets = new[]
        {
            new AssetRecord { Path = "big.fbx", SizeBytes = 10L * 1024 * 1024, Category = AssetCategory.Mesh } // 10 MB
        };
        var report = analyser.Analyse(assets, 100 * 1024 * 1024);

        Assert.Equal(AuditConclusion.Warning, report.Conclusion);
        Assert.Single(report.OverThreshold);
    }

    [Fact]
    public void Analyse_FilesAboveCriticalThreshold_ReturnsFailure()
    {
        var analyser = new AssetSizeAnalyser(_defaultConfig);
        var assets = new[]
        {
            new AssetRecord { Path = "huge.fbx", SizeBytes = 30L * 1024 * 1024, Category = AssetCategory.Mesh } // 30 MB
        };
        var report = analyser.Analyse(assets, 200 * 1024 * 1024);

        Assert.Equal(AuditConclusion.Failure, report.Conclusion);
    }

    [Fact]
    public void Analyse_LargeUnlfsTrackedFiles_RecommendLfs()
    {
        var analyser = new AssetSizeAnalyser(_defaultConfig);
        var assets = new[]
        {
            new AssetRecord { Path = "video.wav", SizeBytes = 20L * 1024 * 1024, IsLfsTracked = false, Category = AssetCategory.Audio },
            new AssetRecord { Path = "lfs.png", SizeBytes = 15L * 1024 * 1024, IsLfsTracked = true, Category = AssetCategory.Texture }
        };
        var report = analyser.Analyse(assets, 100 * 1024 * 1024);

        Assert.Single(report.LfsRecommended); // only the non-lfs tracked one
        Assert.Equal("video.wav", report.LfsRecommended[0].Path);
    }

    [Fact]
    public void Analyse_TopLargestAssets_LimitedTo10()
    {
        var analyser = new AssetSizeAnalyser(_defaultConfig);
        var assets = Enumerable.Range(1, 15)
            .Select(i => new AssetRecord { Path = $"file{i}.fbx", SizeBytes = i * 1024 * 1024 })
            .ToList();
        var report = analyser.Analyse(assets, 1000 * 1024 * 1024);

        Assert.Equal(10, report.TopLargestAssets.Count);
        Assert.Equal("file15.fbx", report.TopLargestAssets[0].Path); // largest first
    }

    [Fact]
    public void Analyse_BinaryAssetPercent_CalculatedCorrectly()
    {
        var analyser = new AssetSizeAnalyser(_defaultConfig);
        var assets = new[]
        {
            new AssetRecord { Path = "a.fbx", SizeBytes = 50 * 1024 * 1024 }
        };
        var report = analyser.Analyse(assets, 200 * 1024 * 1024);

        Assert.Equal(25.0, report.BinaryAssetPercent, precision: 1);
    }
}
