using AssetCoroner.Core.Config;
using AssetCoroner.Core.Models;
using Microsoft.Extensions.Logging;
using Octokit;

namespace AssetCoroner.Core.Audit;

/// <summary>
/// Orchestrates a full repository binary-asset audit by fetching the Git tree from GitHub,
/// classifying blobs, and delegating size analysis to <see cref="AssetSizeAnalyser"/>.
/// </summary>
public class AuditEngine
{
    private readonly IGitHubClient _github;
    private readonly AssetSizeAnalyser _analyser;
    private readonly AuditConfig _config;
    private readonly ILogger<AuditEngine> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="AuditEngine"/>.
    /// </summary>
    /// <param name="github">Authenticated GitHub client used to query repository data.</param>
    /// <param name="config">Audit configuration including size thresholds.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public AuditEngine(IGitHubClient github, AuditConfig config, ILogger<AuditEngine> logger)
    {
        _github = github;
        _config = config;
        _analyser = new AssetSizeAnalyser(config);
        _logger = logger;
    }

    /// <summary>
    /// Fetches the recursive Git tree for <paramref name="sha"/>, identifies binary assets,
    /// and returns a completed <see cref="AuditReport"/>.
    /// </summary>
    /// <param name="owner">GitHub repository owner (user or organisation).</param>
    /// <param name="repo">GitHub repository name.</param>
    /// <param name="sha">Commit SHA to audit.</param>
    public async Task<AuditReport> RunAsync(string owner, string repo, string sha)
    {
        _logger.LogInformation("Running audit on {Owner}/{Repo} @ {Sha}", owner, repo, sha);

        var tree = await _github.Git.Tree.GetRecursive(owner, repo, sha);

        long totalRepoSize = 0;
        var binaryAssets = new List<AssetRecord>();

        foreach (var item in tree.Tree.Where(t => t.Type == TreeType.Blob))
        {
            totalRepoSize += item.Size;

            if (!AssetClassifier.IsBinaryAsset(item.Path))
                continue;

            binaryAssets.Add(new AssetRecord
            {
                Path = item.Path,
                Extension = Path.GetExtension(item.Path),
                SizeBytes = item.Size,
                Sha = item.Sha,
                Category = AssetClassifier.Classify(item.Path),
                IsLfsTracked = IsLfsPointer(item.Size)
            });
        }

        _logger.LogInformation("Found {Count} binary assets out of {Total} tree items",
            binaryAssets.Count, tree.Tree.Count);

        return _analyser.Analyse(binaryAssets, totalRepoSize);
    }

    // LFS pointer files are always exactly 130-140 bytes
    /// <summary>
    /// Returns <c>true</c> when <paramref name="size"/> falls within the byte range
    /// used by Git LFS pointer files (100-200 bytes).
    /// </summary>
    private static bool IsLfsPointer(long size) => size is >= 100 and <= 200;
}
