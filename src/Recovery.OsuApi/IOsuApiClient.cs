using Recovery.Core.Models;
using Recovery.OsuApi.Models;

namespace Recovery.OsuApi;

public interface IOsuApiClient
{
    bool IsAuthenticated { get; }
    Task<bool> AuthenticateAsync(int clientId, string clientSecret, CancellationToken ct = default);
    Task<OsuUser?> ResolveUserAsync(string usernameOrId, CancellationToken ct = default);
    Task<List<BeatmapSet>> DiscoverPlayerLibraryAsync(
        string usernameOrId,
        IEnumerable<Ruleset> rulesets,
        IProgress<string>? progress = null,
        CancellationToken ct = default);
}
