namespace AssetCoroner.Core.Models;

/// <summary>
/// Contains the results of a pull-request binary-asset review, including per-file size deltas
/// and LFS recommendations.
/// </summary>
public class ReviewReport
{
    /// <summary>Gets or sets the list of binary asset changes detected in the pull request.</summary>
    public List<BinaryDelta> Deltas { get; set; } = new();
    /// <summary>Gets the net byte change across all tracked asset files in the pull request.</summary>
    public long TotalDeltaBytes => Deltas.Sum(d => d.DeltaBytes);
    /// <summary>Gets or sets a value indicating whether any asset exceeds the critical size threshold.</summary>
    public bool HasCriticalFiles { get; set; }
    /// <summary>Gets or sets the paths of files recommended for Git LFS storage.</summary>
    public List<string> LfsRecommendations { get; set; } = new();
}
