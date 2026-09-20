using Recovery.Core.Models;
using Recovery.Core.Services;
using Xunit;

namespace Recovery.Tests;

public class DeduplicationTests
{
    [Fact]
    public void Deduplicate_MultipleDifficultiesSameSet_MergesIntoSingleSetWithMergedPlays()
    {
        // Arrange: 3 difficulties of the same SetId: 100
        var set1 = new BeatmapSet
        {
            SetId = 100,
            Artist = "xi",
            Title = "FREEDOM DiVE",
            Mapper = "Nakagawa-Kanon",
            Status = "ranked"
        };
        set1.Difficulties.Add(new BeatmapDifficulty(1001, "Normal", 2.5, 0));
        set1.Evidence.Add(new EvidenceRecord(EvidenceLevel.ConfirmedOnline, "API", "BeatmapId:1001", 10, DateTimeOffset.UtcNow));

        var set2 = new BeatmapSet
        {
            SetId = 100,
            Artist = "xi",
            Title = "FREEDOM DiVE",
            Mapper = "Nakagawa-Kanon",
            Status = "ranked"
        };
        set2.Difficulties.Add(new BeatmapDifficulty(1002, "Hard", 4.0, 0));
        set2.Evidence.Add(new EvidenceRecord(EvidenceLevel.ConfirmedOnline, "API", "BeatmapId:1002", 25, DateTimeOffset.UtcNow));

        var set3 = new BeatmapSet
        {
            SetId = 100,
            Artist = "xi",
            Title = "FREEDOM DiVE",
            Mapper = "Nakagawa-Kanon",
            Status = "ranked"
        };
        set3.Difficulties.Add(new BeatmapDifficulty(1003, "FOUR DIMENSIONS", 7.5, 0));
        set3.Evidence.Add(new EvidenceRecord(EvidenceLevel.ConfirmedOnline, "API", "BeatmapId:1003", 50, DateTimeOffset.UtcNow));

        var input = new List<BeatmapSet> { set1, set2, set3 };

        // Act
        var result = BeatmapSetDeduplicator.Deduplicate(input);

        // Assert
        Assert.Single(result);
        var merged = result[0];
        Assert.Equal(100, merged.SetId);
        Assert.Equal(3, merged.Difficulties.Count);
        Assert.Equal(85, merged.TotalPlayCount); // 10 + 25 + 50
        Assert.Equal(EvidenceLevel.ConfirmedOnline, merged.OverallEvidenceLevel);
    }
}
