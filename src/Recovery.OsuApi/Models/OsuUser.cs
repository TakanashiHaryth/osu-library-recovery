using System.Text.Json.Serialization;

namespace Recovery.OsuApi.Models;

public class OsuUser
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = string.Empty;

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl { get; set; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; set; } = string.Empty;

    [JsonPropertyName("playmode")]
    public string Playmode { get; set; } = "osu";

    [JsonPropertyName("is_supporter")]
    public bool IsSupporter { get; set; }
}
