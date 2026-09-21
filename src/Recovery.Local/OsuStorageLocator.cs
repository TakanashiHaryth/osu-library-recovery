using System.Runtime.InteropServices;

namespace Recovery.Local;

/// <summary>
/// Safely locates and validates osu!lazer storage directories per PRD Section 16.1.
/// Reads storage.ini without modifying any user configuration.
/// </summary>
public static class OsuStorageLocator
{
    /// <summary>
    /// Attempts to automatically discover the user's active osu!lazer storage directory.
    /// Priority:
    /// 1. storage.ini FullPath redirect (e.g. D:\osu-lazer\osu-lazer)
    /// 2. Default platform data directory (%APPDATA%/osu on Windows)
    /// </summary>
    public static string? TryFindOsuStorageDirectory()
    {
        var candidates = GetCandidateOsuDirectories();
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrEmpty(candidate) || !Directory.Exists(candidate))
                continue;

            // Check if storage.ini redirects to a custom directory
            var storageIniPath = Path.Combine(candidate, "storage.ini");
            if (File.Exists(storageIniPath))
            {
                try
                {
                    var lines = File.ReadAllLines(storageIniPath);
                    foreach (var line in lines)
                    {
                        if (line.StartsWith("FullPath", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = line.Split('=', 2);
                            if (parts.Length == 2)
                            {
                                var customPath = parts[1].Trim();
                                if (Directory.Exists(customPath))
                                {
                                    return customPath;
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Silently fallback if storage.ini cannot be read
                }
            }

            if (IsValidOsuStorageDirectory(candidate))
            {
                return candidate;
            }
        }

        var defaultPath = GetDefaultOsuDirectory();
        return Directory.Exists(defaultPath) ? defaultPath : null;
    }

    /// <summary>
    /// Returns ordered candidate paths for osu!lazer across supported platforms.
    /// </summary>
    public static IEnumerable<string> GetCandidateOsuDirectories()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Path.Combine(appData, "osu");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, "Library", "Application Support", "osu");
        }
        else
        {
            // Linux: Check Flatpak first, then native/AppImage ~/.local/share/osu
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Path.Combine(home, ".var", "app", "sh.ppy.osu", "data", "osu");
            yield return Path.Combine(home, ".local", "share", "osu");
        }
    }

    /// <summary>
    /// Gets the standard default platform path for osu!lazer.
    /// </summary>
    public static string GetDefaultOsuDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "osu");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, "Library", "Application Support", "osu");
        }
        else
        {
            // Linux and others
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".local", "share", "osu");
        }
    }

    /// <summary>
    /// Validates if a folder appears to be a legitimate osu!lazer storage directory.
    /// </summary>
    public static bool IsValidOsuStorageDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        var hasFilesDir = Directory.Exists(Path.Combine(path, "files"));
        var hasRealm = File.Exists(Path.Combine(path, "client.realm"));
        var hasStorageIni = File.Exists(Path.Combine(path, "storage.ini"));
        var hasLogs = Directory.Exists(Path.Combine(path, "logs"));

        return (hasFilesDir && hasRealm) || hasStorageIni || (hasFilesDir && hasLogs);
    }

    /// <summary>
    /// Gets the standard default platform path for osu! (Stable).
    /// Typically %LOCALAPPDATA%/osu! on Windows, or ~/.wine/drive_c/osu! on Linux.
    /// </summary>
    public static string GetDefaultOsuStableDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "osu!");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".wine", "drive_c", "osu!");
        }

        return string.Empty;
    }

    /// <summary>
    /// Returns candidate paths for osu! (Stable) across supported platforms including Wine prefixes.
    /// </summary>
    public static IEnumerable<string> GetCandidateOsuStableDirectories()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            yield return Path.Combine(localAppData, "osu!");
            yield return @"C:\osu!";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var user = Environment.UserName;

            // 1. Standard Wine paths
            yield return Path.Combine(home, ".wine", "drive_c", "osu!");
            yield return Path.Combine(home, ".wine", "drive_c", "users", user, "AppData", "Local", "osu!");
            yield return Path.Combine(home, ".local", "share", "osu-wine", "osu!");

            // 2. Bottles paths
            yield return Path.Combine(home, ".var", "app", "com.usebottles.bottles", "data", "bottles", "bottles", "osu", "drive_c", "osu!");
            yield return Path.Combine(home, ".local", "share", "bottles", "bottles", "osu", "drive_c", "osu!");

            // 3. Lutris paths
            yield return Path.Combine(home, "Games", "osu-stable", "drive_c", "osu!");
            yield return Path.Combine(home, "Games", "osu", "drive_c", "osu!");
        }
    }

    /// <summary>
    /// Attempts to discover the user's active osu! (Stable) directory.
    /// Checks candidate locations, Songs subfolder, or osu!.exe.
    /// </summary>
    public static string? TryFindOsuStableDirectory()
    {
        foreach (var candidate in GetCandidateOsuStableDirectories())
        {
            if (IsValidOsuStableDirectory(candidate))
            {
                return candidate;
            }
        }

        var defaultPath = GetDefaultOsuStableDirectory();
        if (!string.IsNullOrEmpty(defaultPath) && IsValidOsuStableDirectory(defaultPath))
        {
            return defaultPath;
        }

        return null;
    }

    /// <summary>
    /// Validates if a folder appears to be a legitimate osu! (Stable) directory.
    /// Checks for Songs directory, osu!.db, or osu!.exe.
    /// </summary>
    public static bool IsValidOsuStableDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return false;
        }

        var hasSongsDir = Directory.Exists(Path.Combine(path, "Songs"));
        var hasDb = File.Exists(Path.Combine(path, "osu!.db"));
        var hasExe = File.Exists(Path.Combine(path, "osu!.exe"));

        return hasSongsDir || hasDb || hasExe;
    }
}
