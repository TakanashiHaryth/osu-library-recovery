using Recovery.Core.Models;

namespace Recovery.Core.Services;

public class SelectionManager
{
    /// <summary>
    /// Selects all sets in the provided visible list.
    /// </summary>
    public static void SelectAll(IEnumerable<BeatmapSet> sets)
    {
        foreach (var set in sets)
        {
            set.IsSelected = true;
        }
    }

    /// <summary>
    /// Deselects all sets in the provided list.
    /// </summary>
    public static void ClearSelection(IEnumerable<BeatmapSet> sets)
    {
        foreach (var set in sets)
        {
            set.IsSelected = false;
        }
    }

    /// <summary>
    /// Inverts the selection state of each set in the provided list.
    /// </summary>
    public static void InvertSelection(IEnumerable<BeatmapSet> sets)
    {
        foreach (var set in sets)
        {
            set.IsSelected = !set.IsSelected;
        }
    }

    /// <summary>
    /// Applies a rule-based selection predicate.
    /// </summary>
    public static void SelectByRule(IEnumerable<BeatmapSet> sets, Func<BeatmapSet, bool> rulePredicate)
    {
        foreach (var set in sets)
        {
            if (rulePredicate(set))
            {
                set.IsSelected = true;
            }
        }
    }

    /// <summary>
    /// Sets download variant (Full or NoVideo) across all selected sets.
    /// </summary>
    public static void SetVariantForSelected(IEnumerable<BeatmapSet> sets, DownloadVariant variant)
    {
        foreach (var set in sets.Where(s => s.IsSelected))
        {
            set.RequestedVariant = variant;
        }
    }
}
