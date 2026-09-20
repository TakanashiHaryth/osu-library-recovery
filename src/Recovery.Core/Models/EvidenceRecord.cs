namespace Recovery.Core.Models;

public record EvidenceRecord(
    EvidenceLevel Level,
    string SourceType,
    string SourceReference,
    int? PlayCount,
    DateTimeOffset DiscoveredAt
);
