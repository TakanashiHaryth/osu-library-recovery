using Recovery.Core.Models;
using Recovery.OsuApi.Models;

namespace Recovery.OsuApi;

public class MockOsuApiClient : IOsuApiClient
{
    public bool IsAuthenticated => true;

    public Task<bool> AuthenticateAsync(int clientId, string clientSecret, CancellationToken ct = default)
    {
        return Task.FromResult(true);
    }

    public Task<OsuUser?> ResolveUserAsync(string usernameOrId, CancellationToken ct = default)
    {
        var user = new OsuUser
        {
            Id = 1234567,
            Username = string.IsNullOrWhiteSpace(usernameOrId) ? "PeppyPlayer" : usernameOrId,
            CountryCode = "MY",
            Playmode = "osu",
            IsSupporter = true,
            AvatarUrl = "https://a.ppy.sh/1234567"
        };
        return Task.FromResult<OsuUser?>(user);
    }

    public async Task<List<BeatmapSet>> DiscoverPlayerLibraryAsync(
        string usernameOrId,
        IEnumerable<Ruleset> rulesets,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        progress?.Report("Mock: Resolving account...");
        await Task.Delay(200, ct);

        progress?.Report("Mock: Fetching Most Played page 1...");
        await Task.Delay(300, ct);

        var list = new List<BeatmapSet>
        {
            new BeatmapSet
            {
                SetId = 252,
                Artist = "Nico Nico Douga",
                Title = "Danjo",
                Mapper = "Andrea",
                Status = "ranked",
                HasVideo = false,
                Difficulties = new List<BeatmapDifficulty>
                {
                    new(100, "Easy", 1.5, 0),
                    new(101, "Normal", 2.3, 0),
                    new(102, "Hard", 3.8, 0)
                },
                Evidence = new List<EvidenceRecord>
                {
                    new(EvidenceLevel.ConfirmedOnline, "API_MostPlayed", "BeatmapId:102", 145, DateTimeOffset.UtcNow)
                }
            },
            new BeatmapSet
            {
                SetId = 112651,
                Artist = "xi",
                Title = "FREEDOM DiVE",
                Mapper = "Nakagawa-Kanon",
                Status = "ranked",
                HasVideo = true,
                Difficulties = new List<BeatmapDifficulty>
                {
                    new(39947, "FOUR DIMENSIONS", 7.5, 0),
                    new(39948, "Another", 6.2, 0)
                },
                Evidence = new List<EvidenceRecord>
                {
                    new(EvidenceLevel.ConfirmedOnline, "API_MostPlayed", "BeatmapId:39947", 89, DateTimeOffset.UtcNow),
                    new(EvidenceLevel.ConfirmedOnline, "API_RecentScore", "Ruleset:Osu", 2, DateTimeOffset.UtcNow)
                }
            },
            new BeatmapSet
            {
                SetId = 39804,
                Artist = "The Quick Brown Fox",
                Title = "The Big Black",
                Mapper = "Blue Dragon",
                Status = "ranked",
                HasVideo = false,
                Difficulties = new List<BeatmapDifficulty>
                {
                    new(131891, "WHO'S AFRAID OF THE BIG BLACK", 6.8, 0)
                },
                Evidence = new List<EvidenceRecord>
                {
                    new(EvidenceLevel.ConfirmedOnline, "API_MostPlayed", "BeatmapId:131891", 210, DateTimeOffset.UtcNow)
                }
            },
            new BeatmapSet
            {
                SetId = 41823,
                Artist = "USAO",
                Title = "Miracle 5ympho X",
                Mapper = "sjoy",
                Status = "loved",
                HasVideo = false,
                Difficulties = new List<BeatmapDifficulty>
                {
                    new(145021, "Extra", 5.9, 1) // Taiko
                },
                Evidence = new List<EvidenceRecord>
                {
                    new(EvidenceLevel.ConfirmedOnline, "API_MostPlayed", "BeatmapId:145021", 34, DateTimeOffset.UtcNow)
                }
            },
            new BeatmapSet
            {
                SetId = 751779,
                Artist = "Camellia",
                Title = "GHOST",
                Mapper = "Mir",
                Status = "ranked",
                HasVideo = true,
                Difficulties = new List<BeatmapDifficulty>
                {
                    new(1582618, "Phantasm", 7.1, 0)
                },
                Evidence = new List<EvidenceRecord>
                {
                    new(EvidenceLevel.ConfirmedOnline, "API_RecentScore", "Ruleset:Osu", 15, DateTimeOffset.UtcNow)
                }
            },
            new BeatmapSet
            {
                SetId = 999999,
                Artist = "Unranked Artist",
                Title = "Graveyard Beatmap",
                Mapper = "UnknownMapper",
                Status = "graveyard",
                HasVideo = false,
                Difficulties = new List<BeatmapDifficulty>
                {
                    new(9999991, "WIP", 4.2, 0)
                },
                Evidence = new List<EvidenceRecord>
                {
                    new(EvidenceLevel.ProbableLocal, "LocalReplayLog", "replay-osu_9999991.osr", 3, DateTimeOffset.UtcNow)
                }
            }
        };

        progress?.Report($"Mock: Discovered {list.Count} beatmap sets.");
        return list;
    }
}
