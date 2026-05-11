using AssetCoroner.Core.Config;
using AssetCoroner.Core.Models;

namespace AssetCoroner.Core.Review;

/// <summary>
/// Evaluates a collection of <see cref="BinaryDelta"/> records against the review configuration
/// and produces a <see cref="ReviewReport"/> with critical-file flags and LFS recommendations.
/// </summary>
public class BinaryDeltaAnalyser
{
    private readonly ReviewConfig _config;

    /// <summary>
    /// Initialises a new instance of <see cref="BinaryDeltaAnalyser"/> with the supplied review configuration.
    /// </summary>
    public BinaryDeltaAnalyser(ReviewConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Inspects each delta in <paramref name="deltas"/>, identifies files that exceed the critical
    /// size threshold, and returns a completed <see cref="ReviewReport"/>.
    /// </summary>
    /// <param name="deltas">Binary asset deltas computed for a single pull request.</param>
    public ReviewReport Analyse(IEnumerable<BinaryDelta> deltas)
    {
        var list = deltas.ToList();
        var criticalBytes = (long)_config.CriticalDeltaMb * 1024 * 1024;

        var lfsRecommendations = list
            .Where(d => d.NewSizeBytes >= criticalBytes && !d.IsLfsPointer)
            .Select(d => d.Path)
            .ToList();

        return new ReviewReport
        {
            Deltas = list,
            HasCriticalFiles = list.Any(d => d.NewSizeBytes >= criticalBytes),
            LfsRecommendations = lfsRecommendations
        };
    }
}
