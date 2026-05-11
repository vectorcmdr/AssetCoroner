using AssetCoroner.Core.Config;
using AssetCoroner.Core.Models;

namespace AssetCoroner.Core.Audit;

/// <summary>
/// Analyses a collection of binary asset records against configured size thresholds
/// and produces an <see cref="AuditReport"/>.
/// </summary>
public class AssetSizeAnalyser
{
    private readonly AuditConfig _config;

    /// <summary>
    /// Initialises a new instance of <see cref="AssetSizeAnalyser"/> with the supplied audit configuration.
    /// </summary>
    public AssetSizeAnalyser(AuditConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Evaluates each asset in <paramref name="assets"/> against the warn and critical thresholds
    /// defined in the configuration and returns a populated <see cref="AuditReport"/>.
    /// </summary>
    /// <param name="assets">All binary assets discovered in the repository tree.</param>
    /// <param name="totalRepoSizeBytes">Total size of all blobs in the repository tree, in bytes.</param>
    public AuditReport Analyse(IEnumerable<AssetRecord> assets, long totalRepoSizeBytes)
    {
        var allBinary = assets.ToList();
        var warnBytes = (long)_config.WarnThresholdMb * 1024 * 1024;
        var criticalBytes = (long)_config.CriticalThresholdMb * 1024 * 1024;
        var lfsBytes = (long)_config.LfsRecommendThresholdMb * 1024 * 1024;

        var overThreshold = allBinary
            .Where(a => a.SizeBytes >= warnBytes)
            .OrderByDescending(a => a.SizeBytes)
            .ToList();

        var lfsRecommended = allBinary
            .Where(a => a.SizeBytes >= lfsBytes && !a.IsLfsTracked)
            .OrderByDescending(a => a.SizeBytes)
            .ToList();

        var topLargest = allBinary
            .OrderByDescending(a => a.SizeBytes)
            .Take(10)
            .ToList();

        var totalBinarySize = allBinary.Sum(a => a.SizeBytes);

        var conclusion = overThreshold.Any(a => a.SizeBytes >= criticalBytes)
            ? AuditConclusion.Failure
            : overThreshold.Any()
                ? AuditConclusion.Warning
                : AuditConclusion.Pass;

        return new AuditReport
        {
            AllBinaryAssets = allBinary,
            OverThreshold = overThreshold,
            LfsRecommended = lfsRecommended,
            TopLargestAssets = topLargest,
            TotalBinarySizeBytes = totalBinarySize,
            TotalRepoSizeBytes = totalRepoSizeBytes,
            Conclusion = conclusion
        };
    }
}
