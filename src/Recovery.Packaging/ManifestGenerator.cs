using System.Text;
using System.Text.Json;
using Recovery.Core.Models;

namespace Recovery.Packaging;

public class ManifestGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    /// <summary>
    /// Generates manifest.json containing machine-readable recovery selection and metadata.
    /// </summary>
    public static string GenerateJsonManifest(IEnumerable<BeatmapSet> sets, string username)
    {
        var manifestObj = new
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Username = username,
            TotalSets = sets.Count(),
            Sets = sets.Select(s => new
            {
                s.SetId,
                s.Artist,
                s.Title,
                s.Mapper,
                s.Status,
                s.HasVideo,
                s.OfficialUrl,
                s.TotalPlayCount,
                EvidenceLevel = s.OverallEvidenceLevel.ToString(),
                s.IsSelected,
                RequestedVariant = s.RequestedVariant.ToString(),
                RecoveryStatus = s.RecoveryStatus.ToString(),
                s.Sha256Hash,
                PackageFile = s.GetSafePackageFileName()
            })
        };

        return JsonSerializer.Serialize(manifestObj, JsonOptions);
    }

    /// <summary>
    /// Generates manifest.csv containing spreadsheet-compatible summary.
    /// </summary>
    public static string GenerateCsvManifest(IEnumerable<BeatmapSet> sets)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SetId,Artist,Title,Mapper,Status,HasVideo,OfficialUrl,TotalPlayCount,EvidenceLevel,Variant,Status,Sha256");

        foreach (var s in sets)
        {
            sb.AppendLine($"{s.SetId},\"{EscapeCsv(s.Artist)}\",\"{EscapeCsv(s.Title)}\",\"{EscapeCsv(s.Mapper)}\",{s.Status},{s.HasVideo},\"{s.OfficialUrl}\",{s.TotalPlayCount},{s.OverallEvidenceLevel},{s.RequestedVariant},{s.RecoveryStatus},{s.Sha256Hash}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Generates recovery-report.txt plain language summary.
    /// </summary>
    public static string GenerateReport(IEnumerable<BeatmapSet> sets, string username)
    {
        var sb = new StringBuilder();
        var setList = sets.ToList();

        sb.AppendLine("==================================================");
        sb.AppendLine("        BEATMAP LIBRARY RECOVERY REPORT           ");
        sb.AppendLine("==================================================");
        sb.AppendLine($"Account: {username}");
        sb.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"Total Discovered Sets: {setList.Count}");
        sb.AppendLine($"Selected Sets: {setList.Count(s => s.IsSelected)}");
        sb.AppendLine($"Completed: {setList.Count(s => s.RecoveryStatus == RecoveryItemStatus.Completed)}");
        sb.AppendLine($"Skipped: {setList.Count(s => s.RecoveryStatus == RecoveryItemStatus.Skipped)}");
        sb.AppendLine($"Unavailable: {setList.Count(s => s.RecoveryStatus == RecoveryItemStatus.Unavailable)}");
        sb.AppendLine($"Failed: {setList.Count(s => s.RecoveryStatus == RecoveryItemStatus.Failed)}");
        sb.AppendLine("--------------------------------------------------");
        sb.AppendLine();

        foreach (var s in setList)
        {
            sb.AppendLine($"[{s.RecoveryStatus}] ID: {s.SetId} | {s.Artist} - {s.Title} ({s.Mapper})");
            sb.AppendLine($"  URL: {s.OfficialUrl} | Evidence: {s.OverallEvidenceLevel} | Plays: {s.TotalPlayCount}");
            if (!string.IsNullOrEmpty(s.FailureReason))
                sb.AppendLine($"  Reason: {s.FailureReason}");
            if (!string.IsNullOrEmpty(s.Sha256Hash))
                sb.AppendLine($"  SHA256: {s.Sha256Hash}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string EscapeCsv(string text) => text.Replace("\"", "\"\"");
}
