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

    /// <summary>
    /// Scans an osu! (Stable) directory for installed beatmap sets.
    /// Inspects the Songs directory and reads folder names and .osu files in read-only mode.
    /// </summary>
    public async Task<LocalLibraryScanResult> ScanStableLibraryAsync(
        string stablePath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new LocalLibraryScanResult();

        if (string.IsNullOrWhiteSpace(stablePath) || !Directory.Exists(stablePath))
        {
            progress?.Report("Directory does not exist.");
            return result;
        }

        var songsDir = Directory.Exists(Path.Combine(stablePath, "Songs"))
            ? Path.Combine(stablePath, "Songs")
            : stablePath;

        if (Directory.Exists(songsDir))
        {
            progress?.Report("Scanning osu! Stable Songs folder...");
            await Task.Run(() => ScanSongsDirectory(songsDir, result, progress, cancellationToken), cancellationToken);
        }
        else
        {
            progress?.Report("No 'Songs' directory found in selected path.");
        }

        progress?.Report($"Scan complete: found {result.InstalledSetIds.Count} installed beatmap sets.");
        return result;
    }

    private void ScanSongsDirectory(
        string songsDir,
        LocalLibraryScanResult result,
        IProgress<string>? progress,
        CancellationToken cancellationToken)
    {
        var folderSetRegex = new Regex(@"^(\d+)\s+", RegexOptions.Compiled);
        int folderCount = 0;

        try
        {
            var subDirs = Directory.GetDirectories(songsDir);
            foreach (var dir in subDirs)
            {
                cancellationToken.ThrowIfCancellationRequested();
                folderCount++;
                var dirName = Path.GetFileName(dir);

                // Quick match from folder name prefix (e.g. "123456 Artist - Title")
                var match = folderSetRegex.Match(dirName);
                if (match.Success && int.TryParse(match.Groups[1].Value, out int setId) && setId > 0)
                {
                    result.InstalledSetIds.Add(setId);
                }

                // Check .osu files inside for metadata and unnumbered folders
                try
                {
                    var osuFiles = Directory.GetFiles(dir, "*.osu");
                    foreach (var osuFile in osuFiles)
                    {
                        result.FilesScanned++;
                        result.BeatmapFilesFound++;

                        using var fs = new FileStream(osuFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        using var reader = new StreamReader(fs, Encoding.UTF8);

                        string? line;
                        int fileSetId = 0;
                        int mapId = 0;
                        string? title = null;
                        string? artist = null;
                        string? version = null;
                        int linesRead = 0;

                        while ((line = reader.ReadLine()) != null && linesRead < 80)
                        {
                            linesRead++;
                            if (fileSetId == 0)
                            {
                                var sm = BeatmapSetIdRegex.Match(line);
                                if (sm.Success && int.TryParse(sm.Groups[1].Value, out int id) && id > 0)
                                    fileSetId = id;
                            }
                            if (mapId == 0)
                            {
                                var bm = BeatmapIdRegex.Match(line);
                                if (bm.Success && int.TryParse(bm.Groups[1].Value, out int id) && id > 0)
                                    mapId = id;
                            }
                            if (title == null)
                            {
                                var tm = TitleRegex.Match(line);
                                if (tm.Success) title = tm.Groups[1].Value.Trim();
                            }
                            if (artist == null)
                            {
                                var am = ArtistRegex.Match(line);
                                if (am.Success) artist = am.Groups[1].Value.Trim();
                            }
                            if (version == null)
                            {
                                var vm = VersionRegex.Match(line);
                                if (vm.Success) version = vm.Groups[1].Value.Trim();
                            }

                            if (line.StartsWith("[HitObjects]", StringComparison.OrdinalIgnoreCase))
                                break;
                        }

                        if (fileSetId > 0)
                        {
                            result.InstalledSetIds.Add(fileSetId);
                            result.DiscoveredBeatmaps.Add(new LocalBeatmapRecord(
                                SetId: fileSetId,
                                BeatmapId: mapId,
                                Title: title ?? "Unknown Title",
                                Artist: artist ?? "Unknown Artist",
                                Version: version ?? "Normal",
                                Md5Hash: null,
                                SourceFile: osuFile
                            ));
                        }
                    }
                }
                catch
                {
                    // Skip unreadable directories
                }

                if (folderCount % 50 == 0)
                {
                    progress?.Report($"Scanning Songs: {folderCount} folders checked ({result.InstalledSetIds.Count} sets found)...");
                }
            }
        }
        catch (Exception ex)
        {
            progress?.Report($"Error reading Songs folder: {ex.Message}");
        }
    }
}
