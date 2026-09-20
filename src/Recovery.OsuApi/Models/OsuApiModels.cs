using System.Text.Json.Serialization;
using Recovery.Core.Models;

namespace Recovery.OsuApi.Models;

public class OsuMostPlayedItem
{
    [JsonPropertyName("beatmap_id")]
    public int BeatmapId { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("beatmap")]
    public OsuBeatmapDetails? Beatmap { get; set; }

    [JsonPropertyName("beatmapset")]
    public OsuBeatmapsetDetails? Beatmapset { get; set; }
}

public class OsuBeatmapDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("beatmapset_id")]
    public int BeatmapsetId { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("difficulty_rating")]
    public double DifficultyRating { get; set; }

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "osu";

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class OsuBeatmapsetDetails
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("artist")]
    public string Artist { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("creator")]
    public string Creator { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("video")]
    public bool Video { get; set; }
}
