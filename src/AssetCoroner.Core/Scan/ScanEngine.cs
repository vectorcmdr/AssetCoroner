using System.Text.RegularExpressions;
using AssetCoroner.Core.Config;
using AssetCoroner.Core.Models;
using Microsoft.Extensions.Logging;
using Octokit;

namespace AssetCoroner.Core.Scan;

/// <summary>
/// Scans Unity asset files changed in a pull request for broken GUID references by building
/// a GUID index from repository .meta files and cross-referencing extracted GUIDs.
/// </summary>
public class ScanEngine
{
    private static readonly HashSet<string> ScanExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".prefab", ".unity", ".asset", ".mat"
    };

    private readonly IGitHubClient _github;
    private readonly ScanConfig _config;
    private readonly UnityYamlParser _parser;
    private readonly ILogger<ScanEngine> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="ScanEngine"/>.
    /// </summary>
    /// <param name="github">Authenticated GitHub client used to query repository and pull-request data.</param>
    /// <param name="config">Scan configuration controlling failure behaviour.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ScanEngine(IGitHubClient github, ScanConfig config, ILogger<ScanEngine> logger)
    {
        _github = github;
        _config = config;
        _parser = new UnityYamlParser();
        _logger = logger;
    }

    /// <summary>
    /// Builds a GUID index from all .meta files in the repository, scans changed Unity asset files
    /// in the pull request for unresolvable GUIDs, and returns a completed <see cref="ScanReport"/>.
    /// </summary>
    /// <param name="owner">GitHub repository owner (user or organisation).</param>
    /// <param name="repo">GitHub repository name.</param>
    /// <param name="pullNumber">Pull request number to scan.</param>
    public async Task<ScanReport> RunAsync(string owner, string repo, int pullNumber)
    {
        _logger.LogInformation("Running scan on PR #{PrNumber} in {Owner}/{Repo}", pullNumber, owner, repo);

        var report = new ScanReport();

        // 1. Build GUID index from repository .meta files
        var pr = await _github.PullRequest.Get(owner, repo, pullNumber);
        var headSha = pr.Head.Sha;

        var guidResolver = await BuildGuidIndexAsync(owner, repo, headSha);

        // 2. Get changed files in the PR
        var files = await _github.PullRequest.Files(owner, repo, pullNumber);
        var changedScanFiles = files
            .Where(f => ScanExtensions.Contains(Path.GetExtension(f.FileName)))
            .ToList();

        _logger.LogInformation("Found {Count} changed Unity asset files to scan", changedScanFiles.Count);

        foreach (var file in changedScanFiles)
        {
            if (file.Status == "removed") continue;

            try
            {
                var content = await GetFileContentAsync(owner, repo, file.FileName, headSha);
                if (string.IsNullOrEmpty(content)) continue;

                if (_parser.IsBinarySerialized(content))
                {
                    report.UnInspectableFiles.Add(file.FileName);
                    continue;
                }

                foreach (var (lineNumber, guid) in _parser.ExtractGuids(content))
                {
                    if (!guidResolver.TryResolve(guid, out _))
                    {
                        report.BrokenReferences.Add(new BrokenReference
                        {
                            FilePath = file.FileName,
                            LineNumber = lineNumber,
                            Guid = guid,
                            Kind = BrokenReferenceKind.MissingGuid,
                            Details = $"GUID {guid} not found in repository .meta index"
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to scan file {File}", file.FileName);
            }
        }

        // Determine conclusion
        report.Conclusion = report.BrokenReferences.Any(r =>
                r.Kind == BrokenReferenceKind.MissingGuid ||
                r.Kind == BrokenReferenceKind.BrokenScriptReference ||
                r.Kind == BrokenReferenceKind.DanglingPrefabVariant)
            ? (_config.FailOnBrokenRefs ? ScanConclusion.Failure : ScanConclusion.Neutral)
            : report.BrokenReferences.Any()
                ? ScanConclusion.Neutral
                : ScanConclusion.Success;

        return report;
    }

    /// <summary>
    /// Fetches all .meta files from the repository tree at <paramref name="sha"/>, extracts GUIDs,
    /// and returns a <see cref="GuidResolver"/> populated with the full GUID index.
    /// </summary>
    private async Task<GuidResolver> BuildGuidIndexAsync(string owner, string repo, string sha)
    {
        var tree = await _github.Git.Tree.GetRecursive(owner, repo, sha);
        var metaFiles = tree.Tree
            .Where(t => t.Type == TreeType.Blob && t.Path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var index = new List<(string assetPath, string guid)>();
        var guidRegex = new Regex(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled);

        foreach (var meta in metaFiles)
        {
            try
            {
                var content = await GetFileContentAsync(owner, repo, meta.Path, sha);
                if (string.IsNullOrEmpty(content)) continue;

                var match = guidRegex.Match(content);
                if (match.Success)
                {
                    var assetPath = meta.Path[..^5]; // remove .meta suffix
                    index.Add((assetPath, match.Groups[1].Value.ToLowerInvariant()));
                }
            }
            catch
            {
                // Skip unreadable meta files
            }
        }

        return new GuidResolver(index);
    }

    /// <summary>
    /// Retrieves the decoded text content of the file at <paramref name="path"/> for the given
    /// <paramref name="sha"/>. Returns <c>null</c> when the file cannot be retrieved.
    /// </summary>
    private async Task<string?> GetFileContentAsync(string owner, string repo, string path, string sha)
    {
        try
        {
            var contents = await _github.Repository.Content.GetAllContentsByRef(owner, repo, path, sha);
            return contents.FirstOrDefault()?.Content is { } content
                ? content
                : null;
        }
        catch
        {
            return null;
        }
    }
}
