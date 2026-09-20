using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Recovery.Core.Models;

namespace Recovery.Local;

/// <summary>
/// Scans local recovery artefacts (logs, replays, manifests) per PRD Section 8 &amp; Functional Requirement DSC 03.
/// Safely extracts evidence without modifying source files.
/// </summary>
public class LocalArtefactScanner
{
    private static readonly Regex BeatmapSetUrlRegex = new(@"beatmapsets/(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BeatmapSetLogRegex = new(@"(?:beatmapset|set_id)\s*[:=]\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Scans a directory for log files and extracts any referenced beatmap set IDs.
    /// </summary>
    public async Task<List<BeatmapSet>> ScanLogsAsync(
        string logsDirectory,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var discoveredMap = new Dictionary<int, BeatmapSet>();

        if (!Directory.Exists(logsDirectory))
        {
            return discoveredMap.Values.ToList();
        }

        var logFiles = Directory.EnumerateFiles(logsDirectory, "*.log", SearchOption.AllDirectories);

        await Task.Run(() =>
        {
            foreach (var logFile in logFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                progress?.Report($"Scanning log file: {Path.GetFileName(logFile)}");

                try
                {
                    using var fs = new FileStream(logFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(fs, Encoding.UTF8);

                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        ExtractSetIdsFromText(line, Path.GetFileName(logFile), discoveredMap);
                    }
                }
                catch
                {
                    // Skip unreadable log files
                }
            }
        }, cancellationToken);

        progress?.Report($"Log scan completed: found {discoveredMap.Count} sets referenced in logs.");
        return discoveredMap.Values.ToList();
    }

    /// <summary>
    /// Scans a directory for .osr replay files and extracts beatmap hash and player metadata.
    /// </summary>
    public async Task<List<LocalBeatmapRecord>> ScanReplaysAsync(
        string replaysDirectory,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<LocalBeatmapRecord>();

        if (!Directory.Exists(replaysDirectory))
        {
            return results;
        }

        var replayFiles = Directory.EnumerateFiles(replaysDirectory, "*.osr", SearchOption.AllDirectories);

        await Task.Run(() =>
        {
            foreach (var replayFile in replayFiles)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    using var fs = new FileStream(replayFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new BinaryReader(fs);

                    // .osr binary format:
                    // byte: ruleset
                    // int32: game version
                    if (fs.Length < 10) continue;
                    reader.ReadByte();
                    reader.ReadInt32();

                    // String: beatmap MD5 hash
                    var beatmapHash = ReadOsuString(reader);
                    // String: player name
                    var playerName = ReadOsuString(reader);

                    if (!string.IsNullOrWhiteSpace(beatmapHash))
                    {
                        results.Add(new LocalBeatmapRecord(
                            SetId: 0,
                            BeatmapId: 0,
                            Title: $"Replay ({playerName})",
                            Artist: "Local Replay",
                            Version: string.Empty,
                            Md5Hash: beatmapHash,
                            SourceFile: replayFile
                        ));
                    }
                }
                catch
                {
                    // Skip invalid replay files
                }
            }
        }, cancellationToken);

        progress?.Report($"Replay scan completed: found {results.Count} replay hashes.");
        return results;
    }

    /// <summary>
    /// Scans previous recovery manifests (JSON) to restore set lists.
    /// </summary>
    public async Task<List<BeatmapSet>> ScanManifestJsonAsync(
        string manifestFilePath,
        IProgress<string>? progress = null)
    {
        var results = new List<BeatmapSet>();

        if (!File.Exists(manifestFilePath))
        {
            return results;
        }

        try
        {
            var json = await File.ReadAllTextAsync(manifestFilePath);
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("requested_sets", out var setsElement) &&
                setsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in setsElement.EnumerateArray())
                {
                    int setId = item.GetProperty("set_id").GetInt32();
                    string artist = item.TryGetProperty("artist", out var a) ? a.GetString() ?? "" : "";
                    string title = item.TryGetProperty("title", out var t) ? t.GetString() ?? "" : "";
                    string mapper = item.TryGetProperty("mapper", out var m) ? m.GetString() ?? "" : "";
                    string status = item.TryGetProperty("status", out var s) ? s.GetString() ?? "" : "unknown";

                    var set = new BeatmapSet
                    {
                        SetId = setId,
                        Artist = artist,
                        Title = title,
                        Mapper = mapper,
                        Status = status,
                        Evidence = new List<EvidenceRecord>
                        {
                            new(EvidenceLevel.ConfirmedLocal, "manifest", $"Manifest: {Path.GetFileName(manifestFilePath)}", null, DateTimeOffset.UtcNow)
                        }
                    };

                    results.Add(set);
                }
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"Failed to parse manifest: {ex.Message}");
        }

        return results;
    }

    private static void ExtractSetIdsFromText(string text, string sourceName, Dictionary<int, BeatmapSet> map)
    {
        var urlMatches = BeatmapSetUrlRegex.Matches(text);
        foreach (Match match in urlMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out int setId) && setId > 0)
            {
                AddDiscoveredSet(setId, sourceName, map);
            }
        }

        var logMatches = BeatmapSetLogRegex.Matches(text);
        foreach (Match match in logMatches)
        {
            if (int.TryParse(match.Groups[1].Value, out int setId) && setId > 0)
            {
                AddDiscoveredSet(setId, sourceName, map);
            }
        }
    }

    private static void AddDiscoveredSet(int setId, string sourceName, Dictionary<int, BeatmapSet> map)
    {
        if (!map.TryGetValue(setId, out var set))
        {
            set = new BeatmapSet
            {
                SetId = setId,
                Title = $"Discovered Set {setId}",
                Artist = "Local Evidence",
                Mapper = "Unknown",
                Status = "discovered",
                Evidence = new List<EvidenceRecord>
                {
                    new(EvidenceLevel.ProbableLocal, "log", $"Log: {sourceName}", 1, DateTimeOffset.UtcNow)
                }
            };
            map[setId] = set;
        }
        else
        {
            set.Evidence.Add(new EvidenceRecord(EvidenceLevel.ProbableLocal, "log", $"Log: {sourceName}", 1, DateTimeOffset.UtcNow));
        }
    }

    private static string ReadOsuString(BinaryReader reader)
    {
        byte flag = reader.ReadByte();
        if (flag != 0x0b) return string.Empty;
        return reader.ReadString();
    }
}
