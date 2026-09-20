using Recovery.Core.Models;

namespace Recovery.Core.Services;

public class BeatmapSetDeduplicator
{
    /// <summary>
    /// Merges multiple raw difficulty or set entries into a deduplicated list of BeatmapSets.
    /// Preserves play counts, merges difficulties, and calculates highest evidence level.
    /// </summary>
    public static List<BeatmapSet> Deduplicate(IEnumerable<BeatmapSet> incomingSets)
    {
        var grouped = incomingSets.GroupBy(s => s.SetId);
        var result = new List<BeatmapSet>();

        foreach (var group in grouped)
        {
            var primary = group.First();

            var mergedSet = new BeatmapSet
            {
                SetId = primary.SetId,
                Artist = string.IsNullOrWhiteSpace(primary.Artist) ? group.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.Artist))?.Artist ?? "Unknown Artist" : primary.Artist,
                Title = string.IsNullOrWhiteSpace(primary.Title) ? group.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.Title))?.Title ?? "Unknown Title" : primary.Title,
                Mapper = string.IsNullOrWhiteSpace(primary.Mapper) ? group.FirstOrDefault(g => !string.IsNullOrWhiteSpace(g.Mapper))?.Mapper ?? "Unknown Mapper" : primary.Mapper,
                Status = primary.Status,
                HasVideo = group.Any(g => g.HasVideo),
                IsSelected = group.Any(g => g.IsSelected),
                RequestedVariant = group.Any(g => g.RequestedVariant == DownloadVariant.NoVideo) ? DownloadVariant.NoVideo : DownloadVariant.Full
            };

            // Merge difficulties by BeatmapId
            var difficulties = group.SelectMany(g => g.Difficulties)
                                    .GroupBy(d => d.BeatmapId)
                                    .Select(dg => dg.First())
                                    .ToList();
            mergedSet.Difficulties = difficulties;

            // Merge evidence
            var evidenceList = group.SelectMany(g => g.Evidence).ToList();
            mergedSet.Evidence = evidenceList;

            result.Add(mergedSet);
        }

        return result;
    }
}
