using System.Text;
using AssetCoroner.Core.Models;

namespace AssetCoroner.Core.Reporting;

/// <summary>
/// Builds Markdown-formatted report strings for audit results, pull-request reviews,
/// and Unity reference scans.
/// </summary>
public static class MarkdownReportBuilder
{
    /// <summary>
    /// Builds a Markdown string summarising the results of a repository binary-asset audit.
    /// </summary>
    /// <param name="report">The completed audit report to format.</param>
    public static string BuildAuditReport(AuditReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 🗂️ AssetCoroner: Audit Report");
        sb.AppendLine();

        var icon = report.Conclusion switch
        {
            AuditConclusion.Pass    => "✅",
            AuditConclusion.Warning => "⚠️",
            AuditConclusion.Failure => "🚨",
            _                       => "ℹ️"
        };
        sb.AppendLine($"**Status:** {icon} {report.Conclusion}");
        sb.AppendLine();
        sb.AppendLine($"- **Total binary asset size:** {FormatBytes(report.TotalBinarySizeBytes)}");
        sb.AppendLine($"- **Total repo size:** {FormatBytes(report.TotalRepoSizeBytes)}");
        sb.AppendLine($"- **Binary asset footprint:** {report.BinaryAssetPercent:F1}%");
        sb.AppendLine($"- **Binary assets found:** {report.AllBinaryAssets.Count}");
        sb.AppendLine();

        if (report.OverThreshold.Any())
        {
            sb.AppendLine("### ⚠️ Files Exceeding Size Thresholds");
            sb.AppendLine();
            sb.AppendLine("| File | Category | Size |");
            sb.AppendLine("|------|----------|------|");
            foreach (var a in report.OverThreshold)
                sb.AppendLine($"| `{a.Path}` | {AssetClassifier.CategoryIcon(a.Category)} {a.Category} | {FormatBytes(a.SizeBytes)} |");
            sb.AppendLine();
        }

        if (report.LfsRecommended.Any())
        {
            sb.AppendLine("### 📦 Git LFS Recommendations");
            sb.AppendLine();
            sb.AppendLine("The following files exceed the LFS threshold and are not yet LFS-tracked:");
            sb.AppendLine();
            foreach (var a in report.LfsRecommended)
                sb.AppendLine($"- `{a.Path}` ({FormatBytes(a.SizeBytes)})");
            sb.AppendLine();
        }

        if (report.TopLargestAssets.Any())
        {
            sb.AppendLine("### 📊 Top Largest Assets");
            sb.AppendLine();
            sb.AppendLine("| Rank | File | Category | Size |");
            sb.AppendLine("|------|------|----------|------|");
            for (int i = 0; i < report.TopLargestAssets.Count; i++)
            {
                var a = report.TopLargestAssets[i];
                sb.AppendLine($"| {i + 1} | `{a.Path}` | {AssetClassifier.CategoryIcon(a.Category)} {a.Category} | {FormatBytes(a.SizeBytes)} |");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a Markdown comment summarising binary asset size changes in a pull request.
    /// Includes per-file deltas, total delta, LFS recommendations, and binary-serialization warnings.
    /// </summary>
    /// <param name="report">The completed review report to format.</param>
    public static string BuildReviewComment(ReviewReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 🔎 AssetCoroner: Binary Asset Post-Mortem");
        sb.AppendLine();
        sb.AppendLine("| File | Category | Change | Previous | New | Delta |");
        sb.AppendLine("|------|----------|--------|----------|-----|-------|");

        foreach (var d in report.Deltas)
        {
            var prev = d.PreviousSizeBytes.HasValue ? FormatBytes(d.PreviousSizeBytes.Value) : "-";
            var next = d.NewSizeBytes.HasValue ? FormatBytes(d.NewSizeBytes.Value) : "-";
            var deltaStr = FormatDelta(d.DeltaBytes);
            var changeIcon = d.ChangeType switch
            {
                DeltaChangeType.New       => "🆕",
                DeltaChangeType.Deletion  => "🗑️",
                DeltaChangeType.Replacement => "🔄",
                _                         => ""
            };
            sb.AppendLine($"| `{d.Path}` | {AssetClassifier.CategoryIcon(d.Category)} {d.Category} | {changeIcon} {d.ChangeType} | {prev} | {next} | {deltaStr} |");
        }

        sb.AppendLine();

        var totalDelta = report.TotalDeltaBytes;
        sb.AppendLine($"**Total PR binary delta: {FormatDelta(totalDelta)}**");

        if (report.HasCriticalFiles)
        {
            sb.AppendLine();
            sb.AppendLine("> 🚨 One or more files exceed the critical size threshold.");
        }

        if (report.LfsRecommendations.Any())
        {
            sb.AppendLine();
            sb.AppendLine("> ⚠️ Consider Git LFS for:");
            foreach (var rec in report.LfsRecommendations)
                sb.AppendLine($"> - `{rec}`");
        }

        var binarySerializedUnityFiles = report.Deltas
            .Where(d => d.IsUnityBinarySerialized == true)
            .Select(d => d.Path)
            .ToList();

        if (binarySerializedUnityFiles.Any())
        {
            sb.AppendLine();
            sb.AppendLine("> ⚠️ The following Unity files are binary-serialized. Consider switching to text serialization for proper version control diffing:");
            foreach (var f in binarySerializedUnityFiles)
                sb.AppendLine($"> - `{f}`");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Builds a Markdown comment summarising the results of a Unity GUID reference scan.
    /// Lists broken references and binary-serialized files that could not be inspected.
    /// </summary>
    /// <param name="report">The completed scan report to format.</param>
    public static string BuildScanComment(ScanReport report)
    {
        var sb = new StringBuilder();
        sb.AppendLine("## 🧩 AssetCoroner: Unity Reference Scan");
        sb.AppendLine();

        var icon = report.Conclusion switch
        {
            ScanConclusion.Success => "✅",
            ScanConclusion.Neutral => "⚠️",
            ScanConclusion.Failure => "🚨",
            _                     => "ℹ️"
        };

        sb.AppendLine($"**Status:** {icon} {report.Conclusion}");
        sb.AppendLine();

        if (!report.BrokenReferences.Any() && !report.UnInspectableFiles.Any())
        {
            sb.AppendLine("✅ No broken Unity references detected.");
            return sb.ToString();
        }

        if (report.BrokenReferences.Any())
        {
            sb.AppendLine("### ❌ Broken References");
            sb.AppendLine();
            sb.AppendLine("| File | Line | GUID | Kind |");
            sb.AppendLine("|------|------|------|------|");
            foreach (var r in report.BrokenReferences)
                sb.AppendLine($"| `{r.FilePath}` | {r.LineNumber} | `{r.Guid}` | {r.Kind} |");
            sb.AppendLine();
        }

        if (report.UnInspectableFiles.Any())
        {
            sb.AppendLine("### ℹ️ Binary-Serialized Files (Cannot Inspect)");
            sb.AppendLine();
            foreach (var f in report.UnInspectableFiles)
                sb.AppendLine($"- `{f}`");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats a byte count as a human-readable string in B, KB, MB, or GB.
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        return bytes switch
        {
            >= 1024 * 1024 * 1024 => $"{bytes / (1024.0 * 1024 * 1024):F2} GB",
            >= 1024 * 1024       => $"{bytes / (1024.0 * 1024):F2} MB",
            >= 1024              => $"{bytes / 1024.0:F1} KB",
            _                    => $"{bytes} B"
        };
    }

    /// <summary>
    /// Formats a signed byte delta as a human-readable string with a leading sign character
    /// and an emoji indicator for increases or decreases.
    /// </summary>
    private static string FormatDelta(long bytes)
    {
        var sign = bytes >= 0 ? "+" : "";
        var icon = bytes > 0 ? "⚠️" : bytes < 0 ? "✅" : "";
        return $"{sign}{FormatBytes(bytes)} {icon}";
    }
}
