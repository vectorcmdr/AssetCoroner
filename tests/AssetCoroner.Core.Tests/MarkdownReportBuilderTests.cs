using AssetCoroner.Core.Models;
using AssetCoroner.Core.Reporting;

namespace AssetCoroner.Core.Tests;

public class MarkdownReportBuilderTests
{
    [Fact]
    public void BuildAuditReport_ContainsHeader()
    {
        var report = new AuditReport { Conclusion = AuditConclusion.Pass };
        var md = MarkdownReportBuilder.BuildAuditReport(report);
        Assert.Contains("AssetCoroner", md);
        Assert.Contains("Audit", md);
    }

    [Fact]
    public void BuildAuditReport_ShowsPassStatus()
    {
        var report = new AuditReport { Conclusion = AuditConclusion.Pass };
        var md = MarkdownReportBuilder.BuildAuditReport(report);
        Assert.Contains("✅", md);
    }

    [Fact]
    public void BuildAuditReport_ShowsFailureStatus()
    {
        var report = new AuditReport { Conclusion = AuditConclusion.Failure };
        var md = MarkdownReportBuilder.BuildAuditReport(report);
        Assert.Contains("🚨", md);
    }

    [Fact]
    public void BuildReviewComment_EmptyDeltas_StillValid()
    {
        var report = new ReviewReport();
        var md = MarkdownReportBuilder.BuildReviewComment(report);
        Assert.Contains("AssetCoroner", md);
        Assert.Contains("Post-Mortem", md);
    }

    [Fact]
    public void BuildReviewComment_ShowsDelta()
    {
        var report = new ReviewReport
        {
            Deltas = new List<BinaryDelta>
            {
                new()
                {
                    Path = "Assets/Models/hero.fbx",
                    Category = AssetCategory.Mesh,
                    ChangeType = DeltaChangeType.Replacement,
                    PreviousSizeBytes = 4 * 1024 * 1024,
                    NewSizeBytes = 6 * 1024 * 1024
                }
            }
        };
        var md = MarkdownReportBuilder.BuildReviewComment(report);
        Assert.Contains("hero.fbx", md);
        Assert.Contains("Mesh", md);
    }

    [Fact]
    public void BuildScanComment_NoIssues_ShowsSuccess()
    {
        var report = new ScanReport { Conclusion = ScanConclusion.Success };
        var md = MarkdownReportBuilder.BuildScanComment(report);
        Assert.Contains("✅", md);
    }

    [Fact]
    public void BuildScanComment_WithBrokenRefs_ShowsTable()
    {
        var report = new ScanReport
        {
            Conclusion = ScanConclusion.Failure,
            BrokenReferences = new List<BrokenReference>
            {
                new()
                {
                    FilePath = "Assets/Prefabs/Player.prefab",
                    LineNumber = 42,
                    Guid = "abc123def456abc123def456abc123de",
                    Kind = BrokenReferenceKind.MissingGuid
                }
            }
        };
        var md = MarkdownReportBuilder.BuildScanComment(report);
        Assert.Contains("Player.prefab", md);
        Assert.Contains("MissingGuid", md);
        Assert.Contains("42", md);
    }

    [Fact]
    public void BuildReviewComment_BinarySerializedUnityFile_ShowsWarning()
    {
        var report = new ReviewReport
        {
            Deltas = new List<BinaryDelta>
            {
                new()
                {
                    Path = "Assets/Prefabs/Enemy.prefab",
                    Category = AssetCategory.Prefab,
                    ChangeType = DeltaChangeType.Replacement,
                    PreviousSizeBytes = 10 * 1024,
                    NewSizeBytes = 12 * 1024,
                    IsUnityBinarySerialized = true,
                }
            }
        };
        var md = MarkdownReportBuilder.BuildReviewComment(report);
        Assert.Contains("Enemy.prefab", md);
        Assert.Contains("binary-serialized", md);
        Assert.Contains("text serialization", md);
    }

    [Fact]
    public void BuildReviewComment_TextSerializedUnityFile_NoWarning()
    {
        var report = new ReviewReport
        {
            Deltas = new List<BinaryDelta>
            {
                new()
                {
                    Path = "Assets/Prefabs/Player.prefab",
                    Category = AssetCategory.Prefab,
                    ChangeType = DeltaChangeType.Replacement,
                    PreviousSizeBytes = 8 * 1024,
                    NewSizeBytes = 9 * 1024,
                    IsUnityBinarySerialized = false,
                }
            }
        };
        var md = MarkdownReportBuilder.BuildReviewComment(report);
        Assert.Contains("Player.prefab", md);
        Assert.DoesNotContain("binary-serialized", md);
    }
}
