namespace Recovery.Local;

/// <summary>
/// Represents a beatmap identified from local osu! files or artefacts.
/// </summary>
public record LocalBeatmapRecord(
    int SetId,
    int BeatmapId,
    string Title,
    string Artist,
    string Version,
    string? Md5Hash,
    string SourceFile
);
