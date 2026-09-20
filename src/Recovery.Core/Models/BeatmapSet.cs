namespace Recovery.Core.Models;

public class BeatmapSet
{
    public int SetId { get; set; }
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Mapper { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool HasVideo { get; set; }
    public string OfficialUrl => $"https://osu.ppy.sh/beatmapsets/{SetId}";

    public List<BeatmapDifficulty> Difficulties { get; set; } = new();
    public List<EvidenceRecord> Evidence { get; set; } = new();

    public int TotalPlayCount => Evidence.Sum(e => e.PlayCount ?? 0);

    public EvidenceLevel OverallEvidenceLevel =>
        Evidence.Count > 0 ? Evidence.Max(e => e.Level) : EvidenceLevel.Unresolved;

    public bool IsSelected { get; set; } = true;
    public bool IsInstalled { get; set; } = false;
    public DownloadVariant RequestedVariant { get; set; } = DownloadVariant.Full;
    public RecoveryItemStatus RecoveryStatus { get; set; } = RecoveryItemStatus.Pending;
    public string? FailureReason { get; set; }
    public string? Sha256Hash { get; set; }
    public long? FileSizeBytes { get; set; }

    /// <summary>
    /// Generates a standardized, sanitized .osz package filename according to PRD section 10.1.
    /// Format: "beatmaps/<set-id> <artist> - <title>.osz"
    /// </summary>
    public string GetSafePackageFileName()
    {
        var safeArtist = SanitizeFileName(Artist);
        var safeTitle = SanitizeFileName(Title);
        return $"{SetId} {safeArtist} - {safeTitle}.osz";
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars).Trim();
    }
}
