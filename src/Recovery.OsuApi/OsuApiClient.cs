using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Recovery.Core.Models;
using Recovery.Core.Services;
using Recovery.OsuApi.Models;

namespace Recovery.OsuApi;

public class OsuApiClient : IOsuApiClient
{
    private readonly HttpClient _httpClient;
    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry;

    public OsuApiClient(HttpClient? httpClient = null)
    {
        if (httpClient != null)
        {
            _httpClient = httpClient;
        }
        else
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = true
            };
            _httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://osu.ppy.sh/")
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }
    }

    public async Task<bool> AuthenticateAsync(int clientId, string clientSecret, CancellationToken ct = default)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "oauth/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId.ToString(),
                ["client_secret"] = clientSecret,
                ["grant_type"] = "client_credentials",
                ["scope"] = "public"
            })
        };

        var response = await SendWithRetryAsync(request, ct);
        if (!response.IsSuccessStatusCode)
            return false;

        var json = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.TryGetProperty("access_token", out var tokenProp))
        {
            _accessToken = tokenProp.GetString();
            int expiresIn = root.TryGetProperty("expires_in", out var expProp) ? expProp.GetInt32() : 86400;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(expiresIn - 60);
            return true;
        }

        return false;
    }

    public async Task<OsuUser?> ResolveUserAsync(string usernameOrId, CancellationToken ct = default)
    {
        // 1. If authenticated, use official API v2 endpoint
        if (IsAuthenticated)
        {
            var apiRequest = new HttpRequestMessage(HttpMethod.Get, $"api/v2/users/{Uri.EscapeDataString(usernameOrId)}");
            AttachAuthHeader(apiRequest);

            var apiResponse = await SendWithRetryAsync(apiRequest, ct);
            if (apiResponse.IsSuccessStatusCode)
            {
                var json = await apiResponse.Content.ReadAsStringAsync(ct);
                return JsonSerializer.Deserialize<OsuUser>(json);
            }
        }

        // 2. Public web resolver (no OAuth required)
        try
        {
            var webRequest = new HttpRequestMessage(HttpMethod.Get, $"users/{Uri.EscapeDataString(usernameOrId)}");
            webRequest.Headers.Add("Accept", "text/html,application/xhtml+xml,application/xml");

            var response = await _httpClient.SendAsync(webRequest, HttpCompletionOption.ResponseHeadersRead, ct);

            // Followed redirect gives the final URL containing numeric user ID (e.g. https://osu.ppy.sh/users/36558455)
            var finalUri = response.RequestMessage?.RequestUri ?? response.Headers.Location;
            if (finalUri != null)
            {
                var segments = finalUri.AbsolutePath.TrimEnd('/').Split('/');
                if (segments.Length > 0 && int.TryParse(segments[^1], out int resolvedId))
                {
                    return new OsuUser
                    {
                        Id = resolvedId,
                        Username = usernameOrId,
                        AvatarUrl = $"https://a.ppy.sh/{resolvedId}"
                    };
                }
            }

            if (int.TryParse(usernameOrId, out int directId))
            {
                return new OsuUser
                {
                    Id = directId,
                    Username = usernameOrId,
                    AvatarUrl = $"https://a.ppy.sh/{directId}"
                };
            }
        }
        catch
        {
            // Public web resolution error fallback
        }

        return null;
    }

    public async Task<List<BeatmapSet>> DiscoverPlayerLibraryAsync(
        string usernameOrId,
        IEnumerable<Ruleset> rulesets,
        IProgress<string>? progress = null,
        CancellationToken ct = default)
    {
        var rawSets = new List<BeatmapSet>();

        // 1. Resolve User
        progress?.Report($"Resolving account '{usernameOrId}'...");
        var user = await ResolveUserAsync(usernameOrId, ct);
        if (user == null)
        {
            progress?.Report($"Could not resolve user '{usernameOrId}'.");
            return new List<BeatmapSet>();
        }

        progress?.Report($"Found user: {user.Username} (ID: {user.Id}). Fetching Most Played...");

        // 2. Fetch Most Played (paginated)
        int offset = 0;
        const int limit = 100;
        int page = 1;

        while (!ct.IsCancellationRequested)
        {
            progress?.Report($"Fetching Most Played page {page} (offset: {offset})...");

            string endpoint = IsAuthenticated
                ? $"api/v2/users/{user.Id}/beatmapsets/most_played?limit={limit}&offset={offset}"
                : $"users/{user.Id}/beatmapsets/most_played?limit={limit}&offset={offset}";

            var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            request.Headers.Add("Accept", "application/json");
            request.Headers.Add("X-Requested-With", "XMLHttpRequest");
            AttachAuthHeader(request);

            var response = await SendWithRetryAsync(request, ct);
            if (!response.IsSuccessStatusCode)
                break;

            var json = await response.Content.ReadAsStringAsync(ct);
            var items = JsonSerializer.Deserialize<List<OsuMostPlayedItem>>(json);

            if (items == null || items.Count == 0)
                break;

            var parsed = OsuApiResponseParser.ParseAndDeduplicateMostPlayed(items);
            rawSets.AddRange(parsed);

            if (items.Count < limit)
                break;

            offset += items.Count;
            page++;
        }

        // 3. Fetch Recent Scores across requested rulesets (if authenticated)
        if (IsAuthenticated)
        {
            foreach (var ruleset in rulesets)
            {
                var modeStr = ruleset switch
                {
                    Ruleset.Taiko => "taiko",
                    Ruleset.Catch => "fruits",
                    Ruleset.Mania => "mania",
                    _ => "osu"
                };

                progress?.Report($"Fetching Recent Scores for {ruleset}...");
                var recentReq = new HttpRequestMessage(HttpMethod.Get, $"api/v2/users/{user.Id}/scores/recent?include_fails=1&mode={modeStr}&limit=50");
                AttachAuthHeader(recentReq);

                var recentRes = await SendWithRetryAsync(recentReq, ct);
                if (recentRes.IsSuccessStatusCode)
                {
                    var recentJson = await recentRes.Content.ReadAsStringAsync(ct);
                    try
                    {
                        using var doc = JsonDocument.Parse(recentJson);
                        if (doc.RootElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var el in doc.RootElement.EnumerateArray())
                            {
                                if (el.TryGetProperty("beatmapset", out var bmsProp) &&
                                    bmsProp.TryGetProperty("id", out var setIdProp))
                                {
                                    int setId = setIdProp.GetInt32();
                                    var bms = new BeatmapSet
                                    {
                                        SetId = setId,
                                        Artist = bmsProp.TryGetProperty("artist", out var a) ? a.GetString() ?? "" : "",
                                        Title = bmsProp.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "",
                                        Mapper = bmsProp.TryGetProperty("creator", out var c) ? c.GetString() ?? "" : "",
                                        Status = bmsProp.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "",
                                        HasVideo = bmsProp.TryGetProperty("video", out var v) && v.GetBoolean()
                                    };
                                    bms.Evidence.Add(new EvidenceRecord(
                                        EvidenceLevel.ConfirmedOnline,
                                        "OsuApi_RecentScore",
                                        $"Ruleset:{ruleset}",
                                        1,
                                        DateTimeOffset.UtcNow
                                    ));
                                    rawSets.Add(bms);
                                }
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        progress?.Report("Deduplicating and merging library evidence...");
        var deduplicated = BeatmapSetDeduplicator.Deduplicate(rawSets);
        progress?.Report($"Discovery complete: {deduplicated.Count} unique beatmap sets identified.");

        return deduplicated;
    }

    private void AttachAuthHeader(HttpRequestMessage request)
    {
        if (!string.IsNullOrEmpty(_accessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, CancellationToken ct)
    {
        int maxRetries = 3;
        int delayMs = 1000;

        for (int i = 0; i < maxRetries; i++)
        {
            HttpResponseMessage response;
            try
            {
                response = await _httpClient.SendAsync(request, ct);
            }
            catch (Exception) when (i < maxRetries - 1)
            {
                await Task.Delay(delayMs, ct);
                delayMs *= 2;
                continue;
            }

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromMilliseconds(delayMs);
                await Task.Delay(retryAfter, ct);
                delayMs *= 2;
                continue;
            }

            return response;
        }

        return new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
    }
}
