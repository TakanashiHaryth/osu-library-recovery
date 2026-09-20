using Recovery.Core.Models;

namespace Recovery.Core.Services;

public record FilterCriteria(
    string? SearchText = null,
    Ruleset? SelectedRuleset = null,
    string? Status = null,
    int? MinPlayCount = null,
    bool? HasVideo = null,
    EvidenceLevel? MinEvidenceLevel = null,
    bool? HideInstalled = null
);

public class BeatmapFilter
{
    public static List<BeatmapSet> ApplyFilter(IEnumerable<BeatmapSet> sets, FilterCriteria criteria)
    {
        var query = sets.AsEnumerable();

        // 1. Text search
        if (!string.IsNullOrWhiteSpace(criteria.SearchText))
        {
            var term = criteria.SearchText.Trim();
            query = query.Where(s =>
                s.SetId.ToString().Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Title.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Artist.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                s.Mapper.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        // 2. Ruleset filter (check if any difficulty in set matches ruleset)
        if (criteria.SelectedRuleset.HasValue)
        {
            int targetMode = (int)criteria.SelectedRuleset.Value;
            query = query.Where(s => s.Difficulties.Any(d => d.RulesetId == targetMode));
        }

        // 3. Status filter
        if (!string.IsNullOrWhiteSpace(criteria.Status) && !criteria.Status.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(s => s.Status.Equals(criteria.Status, StringComparison.OrdinalIgnoreCase));
        }

        // 4. Minimum play count
        if (criteria.MinPlayCount.HasValue && criteria.MinPlayCount.Value > 0)
        {
            query = query.Where(s => s.TotalPlayCount >= criteria.MinPlayCount.Value);
        }

        // 5. Video filter
        if (criteria.HasVideo.HasValue)
        {
            query = query.Where(s => s.HasVideo == criteria.HasVideo.Value);
        }

        // 6. Evidence level
        if (criteria.MinEvidenceLevel.HasValue)
        {
            query = query.Where(s => s.OverallEvidenceLevel >= criteria.MinEvidenceLevel.Value);
        }

        // 7. Hide installed filter
        if (criteria.HideInstalled == true)
        {
            query = query.Where(s => !s.IsInstalled);
        }

        return query.ToList();
    }
}
