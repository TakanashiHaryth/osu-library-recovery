using Recovery.Core.Models;
using Recovery.Core.Services;
using Xunit;

namespace Recovery.Tests;

public class FilterAndSelectionTests
{
    private List<BeatmapSet> CreateSampleSets()
    {
        return new List<BeatmapSet>
        {
            new BeatmapSet
            {
                SetId = 1,
                Artist = "LiSA",
                Title = "Gurenge",
                Mapper = "Nevo",
                Status = "ranked",
                HasVideo = true,
                Difficulties = new List<BeatmapDifficulty> { new(10, "Insane", 4.5, 0) },
                Evidence = new List<EvidenceRecord> { new(EvidenceLevel.ConfirmedOnline, "API", "10", 80, DateTimeOffset.UtcNow) }
            },
            new BeatmapSet
            {
                SetId = 2,
                Artist = "Camellia",
                Title = "Exit This Earth's Atomosphere",
                Mapper = "rrtyui",
                Status = "ranked",
                HasVideo = false,
                Difficulties = new List<BeatmapDifficulty> { new(20, "200step", 6.8, 0) },
                Evidence = new List<EvidenceRecord> { new(EvidenceLevel.ConfirmedOnline, "API", "20", 15, DateTimeOffset.UtcNow) }
            },
            new BeatmapSet
            {
                SetId = 3,
                Artist = "USAO",
                Title = "Cyaegha",
                Mapper = "sjoy",
                Status = "loved",
                HasVideo = false,
                Difficulties = new List<BeatmapDifficulty> { new(30, "Taiko Oni", 5.2, 1) }, // Taiko
                Evidence = new List<EvidenceRecord> { new(EvidenceLevel.ConfirmedOnline, "API", "30", 5, DateTimeOffset.UtcNow) }
            },
            new BeatmapSet
            {
                SetId = 4,
                Artist = "Unknown",
                Title = "Old Graveyard Map",
                Mapper = "MapperX",
                Status = "graveyard",
                HasVideo = false,
                Difficulties = new List<BeatmapDifficulty> { new(40, "Normal", 2.0, 0) },
                Evidence = new List<EvidenceRecord> { new(EvidenceLevel.ProbableLocal, "LocalLog", "40", 1, DateTimeOffset.UtcNow) }
            }
        };
    }

    [Fact]
    public void Filter_BySearchText_MatchesArtistOrTitleOrMapper()
    {
        var sets = CreateSampleSets();

        var resultTitle = BeatmapFilter.ApplyFilter(sets, new FilterCriteria(SearchText: "Gurenge"));
        Assert.Single(resultTitle);
        Assert.Equal(1, resultTitle[0].SetId);

        var resultArtist = BeatmapFilter.ApplyFilter(sets, new FilterCriteria(SearchText: "Camellia"));
        Assert.Single(resultArtist);
        Assert.Equal(2, resultArtist[0].SetId);

        var resultMapper = BeatmapFilter.ApplyFilter(sets, new FilterCriteria(SearchText: "sjoy"));
        Assert.Single(resultMapper);
        Assert.Equal(3, resultMapper[0].SetId);
    }

    [Fact]
    public void Filter_ByRuleset_FiltersCorrectly()
    {
        var sets = CreateSampleSets();

        var taikoSets = BeatmapFilter.ApplyFilter(sets, new FilterCriteria(SelectedRuleset: Ruleset.Taiko));
        Assert.Single(taikoSets);
        Assert.Equal(3, taikoSets[0].SetId);
    }

    [Fact]
    public void Filter_ByMinPlayCount_ExcludesLowPlays()
    {
        var sets = CreateSampleSets();

        var highPlays = BeatmapFilter.ApplyFilter(sets, new FilterCriteria(MinPlayCount: 50));
        Assert.Single(highPlays);
        Assert.Equal(1, highPlays[0].SetId);
    }

    [Fact]
    public void SelectionManager_BulkOperations_WorkCorrectly()
    {
        var sets = CreateSampleSets();

        // 1. Clear Selection
        SelectionManager.ClearSelection(sets);
        Assert.All(sets, s => Assert.False(s.IsSelected));

        // 2. Select All
        SelectionManager.SelectAll(sets);
        Assert.All(sets, s => Assert.True(s.IsSelected));

        // 3. Invert Selection
        sets[0].IsSelected = false;
        SelectionManager.InvertSelection(sets);
        Assert.True(sets[0].IsSelected);
        Assert.False(sets[1].IsSelected);
        Assert.False(sets[2].IsSelected);
        Assert.False(sets[3].IsSelected);

        // 4. Select by Rule (Ranked only)
        SelectionManager.ClearSelection(sets);
        SelectionManager.SelectByRule(sets, s => s.Status == "ranked");
        Assert.Equal(2, sets.Count(s => s.IsSelected));
    }
}
