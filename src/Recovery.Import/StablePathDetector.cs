using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Recovery.Import;

public record StableInstallation(string? ExecutablePath, string? SongsDirectory, bool IsInstalled);

public class StablePathDetector
{
    /// <summary>
    /// Detects osu! (Stable) installation and Songs directory safely.
    /// </summary>
    public static StableInstallation Detect(string? customPath = null)
    {
        // 1. Check custom path if provided by user
        if (!string.IsNullOrWhiteSpace(customPath) && Directory.Exists(customPath))
        {
            var customSongs = Directory.Exists(Path.Combine(customPath, "Songs"))
                ? Path.Combine(customPath, "Songs")
                : (customPath.EndsWith("Songs", StringComparison.OrdinalIgnoreCase) ? customPath : null);

            var customExe = Path.Combine(customPath, "osu!.exe");
            if (File.Exists(customExe))
            {
                var targetSongs = customSongs ?? Path.Combine(customPath, "Songs");
                return new StableInstallation(customExe, targetSongs, true);
            }

            if (customSongs != null)
            {
                return new StableInstallation(null, customSongs, true);
            }
        }

        // 2. Check standard Windows LocalAppData path
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var defaultDir = Path.Combine(localAppData, "osu!");
            if (Directory.Exists(defaultDir))
            {
                var exe = Path.Combine(defaultDir, "osu!.exe");
                var songs = Path.Combine(defaultDir, "Songs");
                return new StableInstallation(
                    File.Exists(exe) ? exe : null,
                    songs,
                    File.Exists(exe) || Directory.Exists(songs)
                );
            }
        }
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var user = Environment.UserName;
            var wineCandidates = new[]
            {
                Path.Combine(home, ".wine", "drive_c", "osu!"),
                Path.Combine(home, ".wine", "drive_c", "users", user, "AppData", "Local", "osu!"),
                Path.Combine(home, ".local", "share", "osu-wine", "osu!"),
                Path.Combine(home, ".var", "app", "com.usebottles.bottles", "data", "bottles", "bottles", "osu", "drive_c", "osu!"),
                Path.Combine(home, ".local", "share", "bottles", "bottles", "osu", "drive_c", "osu!"),
                Path.Combine(home, "Games", "osu-stable", "drive_c", "osu!"),
                Path.Combine(home, "Games", "osu", "drive_c", "osu!")
            };

            foreach (var candidate in wineCandidates)
            {
                if (Directory.Exists(candidate))
                {
                    var exe = Path.Combine(candidate, "osu!.exe");
                    var songs = Path.Combine(candidate, "Songs");
                    if (File.Exists(exe) || Directory.Exists(songs))
                    {
                        return new StableInstallation(
                            File.Exists(exe) ? exe : null,
                            songs,
                            true
                        );
                    }
                }
            }
        }

        // 3. Try Windows registry lookup for osu! file handler
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var regPath = GetExeFromRegistry();
                if (!string.IsNullOrEmpty(regPath) && File.Exists(regPath))
                {
                    var parentDir = Path.GetDirectoryName(regPath);
                    var songs = !string.IsNullOrEmpty(parentDir) ? Path.Combine(parentDir, "Songs") : null;
                    return new StableInstallation(regPath, songs, true);
                }
            }
            catch
            {
                // Fallback
            }
        }

        return new StableInstallation(null, null, false);
    }

    [SupportedOSPlatform("windows")]
    private static string? GetExeFromRegistry()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Classes\osu!\shell\open\command");
        if (key != null)
        {
            var val = key.GetValue(null)?.ToString();
            if (!string.IsNullOrEmpty(val))
            {
                var parts = val.Split('"');
                if (parts.Length > 1 && File.Exists(parts[1]))
                    return parts[1];
            }
        }
        return null;
    }
}
