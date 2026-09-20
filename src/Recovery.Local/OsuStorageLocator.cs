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
        var defaultPath = GetDefaultOsuDirectory();
        if (string.IsNullOrEmpty(defaultPath) || !Directory.Exists(defaultPath))
        {
            return null;
        }

        // Check if storage.ini redirects to a custom directory
        var storageIniPath = Path.Combine(defaultPath, "storage.ini");
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

        return defaultPath;
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
}
