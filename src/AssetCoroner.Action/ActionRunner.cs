using AssetCoroner.Core.Audit;
using AssetCoroner.Core.Config;
using AssetCoroner.Core.Models;
using AssetCoroner.Core.Reporting;
using AssetCoroner.Core.Review;
using AssetCoroner.Core.Scan;
using Microsoft.Extensions.Logging;
using Octokit;

namespace AssetCoroner.Action;

/// <summary>
/// Entry point for the AssetCoroner GitHub Action. Constructs the appropriate analysis
/// engines and coordinates audit, review, and scan runs based on the GitHub event type.
/// </summary>
public class ActionRunner
{
    private readonly IGitHubClient _github;
    private readonly string _owner;
    private readonly string _repo;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<ActionRunner> _logger;

    /// <summary>
    /// Initialises a new instance of <see cref="ActionRunner"/> with the provided GitHub credentials
    /// and repository context.
    /// </summary>
    /// <param name="token">GitHub personal access token or Actions token used for API calls.</param>
    /// <param name="owner">Repository owner (user or organisation).</param>
    /// <param name="repo">Repository name.</param>
    /// <param name="loggerFactory">Factory used to create loggers for each engine.</param>
    public ActionRunner(string token, string owner, string repo, ILoggerFactory loggerFactory)
    {
        _github = new GitHubClient(new ProductHeaderValue("AssetCoroner-Action"))
        {
            Credentials = new Credentials(token)
        };
        _owner = owner;
        _repo = repo;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<ActionRunner>();
    }

    /// <summary>
    /// Runs the repository binary-asset audit for the commit identified by <paramref name="sha"/>,
    /// writes a Markdown step summary, and exits the process with code 1 when the audit fails.
    /// Does nothing when the audit engine is disabled in <paramref name="config"/>.
    /// </summary>
    /// <param name="sha">The commit SHA to audit.</param>
    /// <param name="config">The active AssetCoroner configuration.</param>
    public async Task RunAuditEngineAsync(string sha, AssetCoronerConfig config)
    {
        if (!config.Audit.Enabled)
        {
            _logger.LogInformation("Audit engine is disabled via config.");
            return;
        }

        var engine = new AuditEngine(_github, config.Audit, _loggerFactory.CreateLogger<AuditEngine>());
        var report = await engine.RunAsync(_owner, _repo, sha);

        var markdown = MarkdownReportBuilder.BuildAuditReport(report);
        await WriteStepSummaryAsync(markdown);

        _logger.LogInformation("Audit complete: {Conclusion}, {Count} binary assets, {Over} over threshold",
            report.Conclusion, report.AllBinaryAssets.Count, report.OverThreshold.Count);

        if (report.Conclusion == AuditConclusion.Failure)
        {
            _logger.LogError("Audit FAILED: critical files found above threshold.");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Runs the review and scan engines concurrently for the given pull request, writes Markdown
    /// step summaries for each, and exits the process with code 1 when any engine reports failure.
    /// Individual engines are skipped when disabled in <paramref name="config"/>.
    /// </summary>
    /// <param name="pullNumber">Pull request number to analyse.</param>
    /// <param name="headSha">SHA of the pull request head commit.</param>
    /// <param name="config">The active AssetCoroner configuration.</param>
    public async Task RunPrEnginesAsync(int pullNumber, string headSha, AssetCoronerConfig config)
    {
        var tasks = new List<Task<bool>>();

        if (config.Review.Enabled)
            tasks.Add(RunReviewAsync(pullNumber, headSha, config.Review));

        if (config.Scan.Enabled)
            tasks.Add(RunScanAsync(pullNumber, headSha, config.Scan));

        var results = await Task.WhenAll(tasks);

        if (results.Any(failed => failed))
        {
            _logger.LogError("One or more PR engines reported failure.");
            Environment.Exit(1);
        }
    }

    /// <summary>
    /// Runs the binary-asset review engine for <paramref name="pullNumber"/>, writes a Markdown
    /// step summary, and returns <c>true</c> when the review should block the pull request.
    /// </summary>
    private async Task<bool> RunReviewAsync(int pullNumber, string headSha, ReviewConfig config)
    {
        var engine = new ReviewEngine(_github, config, _loggerFactory.CreateLogger<ReviewEngine>());
        var report = await engine.RunAsync(_owner, _repo, pullNumber);

        var markdown = MarkdownReportBuilder.BuildReviewComment(report);
        await WriteStepSummaryAsync(markdown);

        _logger.LogInformation("Review complete: {Count} binary deltas, total {Delta} bytes",
            report.Deltas.Count, report.TotalDeltaBytes);

        return config.BlockOnCritical && report.HasCriticalFiles;
    }

    /// <summary>
    /// Runs the Unity GUID reference scan for <paramref name="pullNumber"/>, writes a Markdown
    /// step summary, and returns <c>true</c> when the scan concludes with a failure.
    /// </summary>
    private async Task<bool> RunScanAsync(int pullNumber, string headSha, ScanConfig config)
    {
        var engine = new ScanEngine(_github, config, _loggerFactory.CreateLogger<ScanEngine>());
        var report = await engine.RunAsync(_owner, _repo, pullNumber);

        var markdown = MarkdownReportBuilder.BuildScanComment(report);
        await WriteStepSummaryAsync(markdown);

        _logger.LogInformation("Scan complete: {Conclusion}, {Count} broken reference(s)",
            report.Conclusion, report.BrokenReferences.Count);

        return report.Conclusion == ScanConclusion.Failure;
    }

    /// <summary>
    /// Appends <paramref name="markdown"/> to the GitHub Actions step summary file when the
    /// <c>GITHUB_STEP_SUMMARY</c> environment variable is set, or writes it to standard output otherwise.
    /// </summary>
    private static async Task WriteStepSummaryAsync(string markdown)
    {
        var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrEmpty(summaryPath))
            await File.AppendAllTextAsync(summaryPath, markdown + "\n\n");
        else
            Console.WriteLine(markdown);
    }
}
