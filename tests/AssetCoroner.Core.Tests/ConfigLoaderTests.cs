using AssetCoroner.Core.Config;

namespace AssetCoroner.Core.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void Load_NullYaml_ReturnsDefaults()
    {
        var config = ConfigLoader.Load(null);
        Assert.True(config.Audit.Enabled);
        Assert.True(config.Scan.Enabled);
        Assert.True(config.Review.Enabled);
        Assert.Equal(5, config.Audit.WarnThresholdMb);
        Assert.Equal(25, config.Audit.CriticalThresholdMb);
    }

    [Fact]
    public void Load_EmptyYaml_ReturnsDefaults()
    {
        var config = ConfigLoader.Load(string.Empty);
        Assert.Equal(5, config.Audit.WarnThresholdMb);
    }

    [Fact]
    public void Load_InvalidYaml_ReturnsDefaults()
    {
        var config = ConfigLoader.Load(":::invalid yaml:::");
        Assert.NotNull(config);
        Assert.True(config.Audit.Enabled);
    }

    [Fact]
    public void Load_ValidYaml_OverridesDefaults()
    {
        var yaml = @"
audit:
  enabled: true
  warn_threshold_mb: 2
  critical_threshold_mb: 10
  lfs_recommend_threshold_mb: 5
  report_commit: true
scan:
  enabled: false
  fail_on_broken_refs: false
review:
  enabled: true
  block_on_critical: true
  critical_delta_mb: 20
";
        var config = ConfigLoader.Load(yaml);
        Assert.Equal(2, config.Audit.WarnThresholdMb);
        Assert.Equal(10, config.Audit.CriticalThresholdMb);
        Assert.True(config.Audit.ReportCommit);
        Assert.False(config.Scan.Enabled);
        Assert.False(config.Scan.FailOnBrokenRefs);
        Assert.True(config.Review.BlockOnCritical);
        Assert.Equal(20, config.Review.CriticalDeltaMb);
    }
}
