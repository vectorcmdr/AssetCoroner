using AssetCoroner.Core.Config;
using AssetCoroner.Core.Models;
using AssetCoroner.Core.Scan;
using Microsoft.Extensions.Logging;
using Octokit;

namespace AssetCoroner.Core.Review;

/// <summary>
/// Orchestrates a pull-request binary-asset review by fetching changed files from GitHub,
/// computing per-file size deltas, and delegating threshold analysis to <see cref="BinaryDeltaAnalyser"/>.
/// </summary>
public class ReviewEngine
{
    private readonly IGitHubClient _github;
    private readonly ReviewConfig _config;
    private readonly BinaryDeltaAnalyser _analyser;
    // Parses Unity asset files to detect binary vs. text (YAML) serialization mode
    private readonly UnityYamlParser _unityParser;
    private readonly ILogger<ReviewEngine> _logger;

    // LFS pointer magic prefix
    private const string LfsPointerMagic = "version https://git-lfs.github.com/spec";

    /// <summary>
    /// Initialises a new instance of <see cref="ReviewEngine"/>.
    /// </summary>
    /// <param name="github">Authenticated GitHub client used to query pull-request data.</param>
    /// <param name="config">Review configuration including the critical size threshold and tracked extensions.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ReviewEngine(IGitHubClient github, ReviewConfig config, ILogger<ReviewEngine> logger)
    {
        _github = github;
        _config = config;
        _analyser = new BinaryDeltaAnalyser(config);
        _unityParser = new UnityYamlParser();
        _logger = logger;
    }

    /// <summary>
    /// Fetches the changed files for <paramref name="pullNumber"/>, computes a <see cref="BinaryDelta"/>
    /// for each tracked binary asset, and returns a completed <see cref="ReviewReport"/>.
    /// </summary>
    /// <param name="owner">GitHub repository owner (user or organisation).</param>
    /// <param name="repo">GitHub repository name.</param>
    /// <param name="pullNumber">Pull request number to review.</param>
    public async Task<ReviewReport> RunAsync(string owner, string repo, int pullNumber)
    {
        _logger.LogInformation("Running review on PR #{PrNumber} in {Owner}/{Repo}", pullNumber, owner, repo);

        var pr = await _github.PullRequest.Get(owner, repo, pullNumber);
        var files = await _github.PullRequest.Files(owner, repo, pullNumber);

        var trackedFiles = files
            .Where(f => AssetClassifier.IsBinaryAsset(f.FileName, _config.Extensions))
            .ToList();

        _logger.LogInformation("Found {Count} tracked asset changes in PR #{PrNumber}", trackedFiles.Count, pullNumber);

        var deltas = new List<BinaryDelta>();

        foreach (var file in trackedFiles)
        {
            try
            {
                var delta = await BuildDeltaAsync(owner, repo, file, pr.Base.Sha, pr.Head.Sha);
                if (delta != null)
                    deltas.Add(delta);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to compute delta for {File}", file.FileName);
            }
        }

        return _analyser.Analyse(deltas);
    }

    /// <summary>
    /// Builds a <see cref="BinaryDelta"/> for a single pull-request file by fetching blob sizes
    /// from the base and head commits. Returns <c>null</c> when the file is detected as
    /// a text-format variant of a dual-format extension (e.g. ASCII FBX or ASCII STL).
    /// </summary>
    private async Task<BinaryDelta?> BuildDeltaAsync(
        string owner, string repo,
        PullRequestFile file,
        string baseSha, string headSha)
    {
        long? prevSize = null;
        long? newSize = null;
        bool isLfs = false;
        string? prevContent = null;
        string? newContent = null;

        if (file.Status != "added")
        {
            var (size, prevIsLfs, content) = await GetBlobSizeAsync(owner, repo, file.FileName, baseSha);
            prevSize = size;
            isLfs |= prevIsLfs;
            prevContent = content;
        }

        if (file.Status != "removed")
        {
            var (size, newIsLfs, content) = await GetBlobSizeAsync(owner, repo, file.FileName, headSha);
            newSize = size;
            isLfs |= newIsLfs;
            newContent = content;
        }

        // For dual-format files (FBX/STL) that are ASCII/text, skip binary delta tracking.
        // ASCII variants can be diffed by Git normally and do not need size tracking here.
        var contentToCheck = file.Status == "removed" ? prevContent : newContent;
        if (AssetClassifier.IsDualFormatExtension(file.FileName))
        {
            var ext = Path.GetExtension(file.FileName);
            bool isBinary = ext.Equals(".fbx", StringComparison.OrdinalIgnoreCase)
                ? AssetClassifier.LooksLikeBinaryFbx(contentToCheck ?? string.Empty)
                : AssetClassifier.LooksLikeBinaryStl(contentToCheck ?? string.Empty);

            if (!isBinary)
            {
                _logger.LogInformation("Skipping {File} - detected as ASCII text format", file.FileName);
                return null;
            }
        }

        // For Unity-serialized files, detect whether binary or text serialization is in use.
        bool? isUnityBinarySerialized = null;
        if (AssetClassifier.IsUnitySerializedFormat(file.FileName))
        {
            isUnityBinarySerialized = _unityParser.IsBinarySerialized(contentToCheck ?? string.Empty);
        }

        var changeType = file.Status switch
        {
            "added"   => DeltaChangeType.New,
            "removed" => DeltaChangeType.Deletion,
            _         => DeltaChangeType.Replacement
        };

        return new BinaryDelta
        {
            Path = file.FileName,
            Category = AssetClassifier.Classify(file.FileName),
            ChangeType = changeType,
            PreviousSizeBytes = prevSize,
            NewSizeBytes = newSize,
            IsLfsPointer = isLfs,
            IsUnityBinarySerialized = isUnityBinarySerialized,
        };
    }

    /// <summary>
    /// Retrieves the blob at <paramref name="path"/> for the given <paramref name="sha"/> and returns
    /// its size, whether it is an LFS pointer, and its decoded text content.
    /// Returns <c>(null, false, null)</c> when the file cannot be retrieved.
    /// </summary>
    private async Task<(long? size, bool isLfsPointer, string? content)> GetBlobSizeAsync(
        string owner, string repo, string path, string sha)
    {
        try
        {
            var contents = await _github.Repository.Content.GetAllContentsByRef(owner, repo, path, sha);
            var item = contents.FirstOrDefault();
            if (item == null) return (null, false, null);

            // Detect LFS pointer: Content is the decoded text from the API
            var content = item.Content;
            if (content != null && content.StartsWith(LfsPointerMagic, StringComparison.Ordinal))
            {
                // Parse 'size' field from LFS pointer
                var sizeMatch = System.Text.RegularExpressions.Regex.Match(content, @"size (\d+)");
                if (sizeMatch.Success && long.TryParse(sizeMatch.Groups[1].Value, out var lfsSize))
                    return (lfsSize, true, content);
                return (null, true, content);
            }

            return (item.Size, false, content);
        }
        catch
        {
            return (null, false, null);
        }
    }
}
