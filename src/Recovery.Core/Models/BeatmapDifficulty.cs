namespace Recovery.Core.Models;

public record BeatmapDifficulty(
    int BeatmapId,
    string Version,
    double StarRating,
    int RulesetId
);
