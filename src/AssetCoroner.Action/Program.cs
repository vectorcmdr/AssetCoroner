using AssetCoroner.Core.Config;
using AssetCoroner.Core.Reporting;
using AssetCoroner.Action;
using Microsoft.Extensions.Logging;

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Information);
});
var logger = loggerFactory.CreateLogger("AssetCoroner.Action");

// GitHub Actions sets these environment variables
var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN")
                  ?? throw new InvalidOperationException("GITHUB_TOKEN environment variable is required");
var githubRepository = Environment.GetEnvironmentVariable("GITHUB_REPOSITORY")
                       ?? throw new InvalidOperationException("GITHUB_REPOSITORY environment variable is required");
var githubSha = Environment.GetEnvironmentVariable("GITHUB_SHA")
                ?? throw new InvalidOperationException("GITHUB_SHA environment variable is required");
var prNumberStr = Environment.GetEnvironmentVariable("GITHUB_PR_NUMBER");
var configPath = Environment.GetEnvironmentVariable("ASSETCORONER_CONFIG") ?? ".github/assetcoroner.yml";

logger.LogInformation("AssetCoroner Action starting on {Repository}@{Sha}", githubRepository, githubSha);

// Parse owner/repo
var parts = githubRepository.Split('/');
if (parts.Length != 2)
    throw new InvalidOperationException($"Invalid GITHUB_REPOSITORY format: {githubRepository}");
var owner = parts[0];
var repo  = parts[1];

// Load config
var configYaml = File.Exists(configPath) ? await File.ReadAllTextAsync(configPath) : null;
var config = ConfigLoader.Load(configYaml);

var runner = new ActionRunner(githubToken, owner, repo, loggerFactory);

// Determine event type
if (prNumberStr != null && int.TryParse(prNumberStr, out var prNumber))
{
    logger.LogInformation("Running PR engines on PR #{PrNumber}", prNumber);
    await runner.RunPrEnginesAsync(prNumber, githubSha, config);
}
else
{
    logger.LogInformation("Running push/audit engine");
    await runner.RunAuditEngineAsync(githubSha, config);
}

logger.LogInformation("AssetCoroner Action complete.");
