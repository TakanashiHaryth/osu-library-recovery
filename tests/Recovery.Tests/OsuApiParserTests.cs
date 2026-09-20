using System.Text.Json;
using Recovery.Core.Models;
using Recovery.OsuApi;
using Recovery.OsuApi.Models;
using Xunit;

namespace Recovery.Tests;

public class OsuApiParserTests
{
    [Fact]
    public void ParseAndDeduplicateMostPlayed_ParsesJsonAndAggregatesCorrectly()
    {
        var sampleJson = """
        [
          {
            "beatmap_id": 1001,
            "count": 42,
            "beatmap": {
              "id": 1001,
              "beatmapset_id": 500,
              "version": "Hard",
              "difficulty_rating": 3.8,
              "mode": "osu",
              "status": "ranked"
            },
            "beatmapset": {
              "id": 500,
              "artist": "Reol",
              "title": "No title",
              "creator": "LoliGoth",
              "status": "ranked",
              "video": true
            }
          },
          {
            "beatmap_id": 1002,
            "count": 18,
            "beatmap": {
              "id": 1002,
              "beatmapset_id": 500,
              "version": "Insane",
              "difficulty_rating": 4.9,
              "mode": "osu",
              "status": "ranked"
            },
            "beatmapset": {
              "id": 500,
              "artist": "Reol",
              "title": "No title",
              "creator": "LoliGoth",
              "status": "ranked",
              "video": true
            }
          }
        ]
        """;

        var items = JsonSerializer.Deserialize<List<OsuMostPlayedItem>>(sampleJson);
        Assert.NotNull(items);

        var sets = OsuApiResponseParser.ParseAndDeduplicateMostPlayed(items);

        Assert.Single(sets);
        var set = sets[0];
        Assert.Equal(500, set.SetId);
        Assert.Equal("Reol", set.Artist);
        Assert.Equal("No title", set.Title);
        Assert.True(set.HasVideo);
        Assert.Equal(2, set.Difficulties.Count);
        Assert.Equal(60, set.TotalPlayCount); // 42 + 18
        Assert.Equal(EvidenceLevel.ConfirmedOnline, set.OverallEvidenceLevel);
    }
}
