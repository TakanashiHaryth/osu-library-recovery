using System.Text;
using System.Text.RegularExpressions;

namespace Recovery.Local;

public class LocalLibraryScanResult
{
    public HashSet<int> InstalledSetIds { get; } = new();
    public List<LocalBeatmapRecord> DiscoveredBeatmaps { get; } = new();
    public int FilesScanned { get; set; }
    public int BeatmapFilesFound { get; set; }
}

/// <summary>
/// Scans the local osu!lazer storage directory in strictly read-only mode per PRD Section 16.1.
/// Never writes, alters, or locks files in client.realm or the files/ content store.
/// </summary>
public class LocalLibraryScanner
{
    private static readonly Regex BeatmapSetIdRegex = new(@"^BeatmapSetID\s*:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex BeatmapIdRegex = new(@"^BeatmapID\s*:\s*(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TitleRegex = new(@"^Title\s*:\s*(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex ArtistRegex = new(@"^Artist\s*:\s*(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex VersionRegex = new(@"^Version\s*:\s*(.+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Scans the given osu!lazer directory for installed beatmap sets.
    /// Reads .osu files from the files/ directory using non-exclusive read-only file streams.
    /// </summary>
    public async Task<LocalLibraryScanResult> ScanInstalledLibraryAsync(
        string storagePath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new LocalLibraryScanResult();

        if (string.IsNullOrWhiteSpace(storagePath) || !Directory.Exists(storagePath))
        {
            progress?.Report("Storage directory does not exist.");
            return result;
        }

        var filesDir = Path.Combine(storagePath, "files");
        if (Directory.Exists(filesDir))
        {
            progress?.Report("Scanning files store for installed beatmaps...");
            await Task.Run(() => ScanFilesDirectory(filesDir, result, progress, cancellationToken), cancellationToken);
        }
        else
        {
            progress?.Report("No 'files' directory found in selected storage path.");
        }

        // Also check client.realm in read-only stream mode for additional Set IDs
        var realmPath = Path.Combine(storagePath, "client.realm");
        if (File.Exists(realmPath))
        {
            try
            {
                progress?.Report("Scanning client.realm (read-only) for extra set IDs...");
                await Task.Run(() => ScanRealmStrings(realmPath, result, cancellationToken), cancellationToken);
            }
            catch
            {
                // Silently ignore if realm file is unavailable
            }
        }

        progress?.Report($"Scan complete: found {result.InstalledSetIds.Count} installed beatmap sets.");
        return result;
    }

    private void ScanFilesDirectory(
        string filesDir,
        LocalLibraryScanResult result,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        // Enumerate all files in files directory
        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };

        var allFiles = Directory.EnumerateFiles(filesDir, "*", options);
        int count = 0;

        foreach (var filePath in allFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            count++;

            if (count % 200 == 0)
            {
                progress?.Report($"Scanning files: checked {count} items ({result.InstalledSetIds.Count} beatmap sets found)...");
            }

            try
            {
                // Open read-only with full sharing so running osu! instance is never disturbed
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096);
                using var reader = new StreamReader(fs, Encoding.UTF8, true, 4096);

                var firstLine = reader.ReadLine();
                if (firstLine == null || !firstLine.StartsWith("osu file format v", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // This is a .osu beatmap file!
                result.BeatmapFilesFound++;

                int setId = 0;
                int mapId = 0;
                string title = string.Empty;
                string artist = string.Empty;
                string version = string.Empty;

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    // If we passed the Metadata section, break early
                    if (line.StartsWith("[Difficulty]", StringComparison.OrdinalIgnoreCase) ||
                        line.StartsWith("[Events]", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }

                    var setMatch = BeatmapSetIdRegex.Match(line);
                    if (setMatch.Success && int.TryParse(setMatch.Groups[1].Value, out int sid))
                    {
                        setId = sid;
                        continue;
                    }

                    var mapMatch = BeatmapIdRegex.Match(line);
                    if (mapMatch.Success && int.TryParse(mapMatch.Groups[1].Value, out int mid))
                    {
                        mapId = mid;
                        continue;
                    }

                    var titleMatch = TitleRegex.Match(line);
                    if (titleMatch.Success && string.IsNullOrEmpty(title))
                    {
                        title = titleMatch.Groups[1].Value.Trim();
                        continue;
                    }

                    var artistMatch = ArtistRegex.Match(line);
                    if (artistMatch.Success && string.IsNullOrEmpty(artist))
                    {
                        artist = artistMatch.Groups[1].Value.Trim();
                        continue;
                    }

                    var verMatch = VersionRegex.Match(line);
                    if (verMatch.Success && string.IsNullOrEmpty(version))
                    {
                        version = verMatch.Groups[1].Value.Trim();
                        continue;
                    }
                }

                if (setId > 0)
                {
                    result.InstalledSetIds.Add(setId);
                    result.DiscoveredBeatmaps.Add(new LocalBeatmapRecord(
                        SetId: setId,
                        BeatmapId: mapId,
                        Title: title,
                        Artist: artist,
                        Version: version,
                        Md5Hash: null,
                        SourceFile: filePath
                    ));
                }
            }
            catch
            {
                // Skip unreadable files
            }
        }

        result.FilesScanned = count;
    }

    private void ScanRealmStrings(string realmPath, LocalLibraryScanResult result, CancellationToken cancellationToken)
    {
        // Safe read-only heuristic extraction: find sequences of "BeatmapSetID" or "beatmapsets/<id>"
        using var fs = new FileStream(realmPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64 * 1024);
        using var reader = new StreamReader(fs, Encoding.ASCII);

        char[] buffer = new char[64 * 1024];
        int read;
        var setUrlRegex = new Regex(@"beatmapsets/(\d+)", RegexOptions.Compiled);

        while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var text = new string(buffer, 0, read);
            var matches = setUrlRegex.Matches(text);
            foreach (Match match in matches)
            {
                if (int.TryParse(match.Groups[1].Value, out int id) && id > 0)
                {
                    result.InstalledSetIds.Add(id);
                }
            }
        }
    }
}
