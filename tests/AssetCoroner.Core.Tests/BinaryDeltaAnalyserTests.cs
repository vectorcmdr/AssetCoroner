using AssetCoroner.Core.Config;
using AssetCoroner.Core.Models;
using AssetCoroner.Core.Review;

namespace AssetCoroner.Core.Tests;

public class BinaryDeltaAnalyserTests
{
    private readonly ReviewConfig _config = new()
    {
        CriticalDeltaMb = 10,
        BlockOnCritical = false
    };

    [Fact]
    public void Analyse_EmptyDeltas_ReturnsEmptyReport()
    {
        var analyser = new BinaryDeltaAnalyser(_config);
        var report = analyser.Analyse(Enumerable.Empty<BinaryDelta>());

        Assert.Empty(report.Deltas);
        Assert.Equal(0, report.TotalDeltaBytes);
        Assert.False(report.HasCriticalFiles);
    }

    [Fact]
    public void Analyse_LargeNewFile_MarkedCritical()
    {
        var analyser = new BinaryDeltaAnalyser(_config);
        var deltas = new[]
        {
            new BinaryDelta
            {
                Path = "huge.wav",
                ChangeType = DeltaChangeType.New,
                NewSizeBytes = 20L * 1024 * 1024, // 20 MB > 10 MB critical
                Category = AssetCategory.Audio
            }
        };
        var report = analyser.Analyse(deltas);

        Assert.True(report.HasCriticalFiles);
        Assert.Contains("huge.wav", report.LfsRecommendations);
    }

    [Fact]
    public void Analyse_LargeFileAlreadyLfs_NotRecommended()
    {
        var analyser = new BinaryDeltaAnalyser(_config);
        var deltas = new[]
        {
            new BinaryDelta
            {
                Path = "huge.wav",
                ChangeType = DeltaChangeType.Replacement,
                NewSizeBytes = 20L * 1024 * 1024,
                IsLfsPointer = true,
                Category = AssetCategory.Audio
            }
        };
        var report = analyser.Analyse(deltas);

        Assert.True(report.HasCriticalFiles);
        Assert.DoesNotContain("huge.wav", report.LfsRecommendations);
    }

    [Fact]
    public void Analyse_DeltaBytes_CalculatedCorrectly()
    {
        var delta = new BinaryDelta
        {
            PreviousSizeBytes = 4 * 1024 * 1024,
            NewSizeBytes = 6 * 1024 * 1024
        };
        Assert.Equal(2 * 1024 * 1024, delta.DeltaBytes);
    }

    [Fact]
    public void Analyse_DeltaPercent_CalculatedCorrectly()
    {
        var delta = new BinaryDelta
        {
            PreviousSizeBytes = 4 * 1024 * 1024,
            NewSizeBytes = 6 * 1024 * 1024
        };
        Assert.Equal(50.0, delta.DeltaPercent, precision: 1);
    }
}
