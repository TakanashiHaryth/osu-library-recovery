using Recovery.Core.Models;
using Recovery.Core.Services;
using Recovery.OsuApi.Models;

namespace Recovery.OsuApi;

public class OsuApiResponseParser
{
    /// <summary>
    /// Converts a list of raw API Most Played items into domain BeatmapSets and deduplicates them.
    /// Preserves individual difficulty metadata and sets EvidenceLevel to ConfirmedOnline.
    /// </summary>
    public static List<BeatmapSet> ParseAndDeduplicateMostPlayed(IEnumerable<OsuMostPlayedItem> items)
    {
        var rawSets = new List<BeatmapSet>();

        foreach (var item in items)
        {
            if (item.Beatmapset == null)
                continue;

            var set = new BeatmapSet
            {
                SetId = item.Beatmapset.Id,
                Artist = item.Beatmapset.Artist,
                Title = item.Beatmapset.Title,
                Mapper = item.Beatmapset.Creator,
                Status = item.Beatmapset.Status,
                HasVideo = item.Beatmapset.Video,
                IsSelected = true
            };

            if (item.Beatmap != null)
            {
                int rulesetId = ParseRuleset(item.Beatmap.Mode);
                set.Difficulties.Add(new BeatmapDifficulty(
                    item.Beatmap.Id,
                    item.Beatmap.Version,
                    item.Beatmap.DifficultyRating,
                    rulesetId
                ));
            }

            set.Evidence.Add(new EvidenceRecord(
                EvidenceLevel.ConfirmedOnline,
                "OsuApi_MostPlayed",
                $"BeatmapId:{item.BeatmapId}",
                item.Count,
                DateTimeOffset.UtcNow
            ));

            rawSets.Add(set);
        }

        return BeatmapSetDeduplicator.Deduplicate(rawSets);
    }

    private static int ParseRuleset(string mode) => mode.ToLowerInvariant() switch
    {
        "taiko" => 1,
        "fruits" or "catch" => 2,
        "mania" => 3,
        _ => 0
    };
}
