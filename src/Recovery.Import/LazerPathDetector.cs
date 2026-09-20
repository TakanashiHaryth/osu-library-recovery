using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Recovery.Import;

public record LazerInstallation(string? ExecutablePath, string? DataDirectory, bool IsInstalled);

public class LazerPathDetector
{
    /// <summary>
    /// Detects osu!lazer installation path and data directory on the machine safely.
    /// </summary>
    public static LazerInstallation Detect()
    {
        string? exePath = FindExecutable();
        string? dataDir = FindDataDirectory();

        return new LazerInstallation(exePath, dataDir, !string.IsNullOrEmpty(exePath) && File.Exists(exePath));
    }

    private static string? FindExecutable()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // Check default Velopack / Squirrel lazer paths
        var candidatePaths = new[]
        {
            Path.Combine(localAppData, "osulazer", "osu!.exe"),
            Path.Combine(localAppData, "osulazer", "current", "osu!.exe"),
            Path.Combine(localAppData, "osu", "osu!.exe"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "osu!", "osu!.exe")
        };

        foreach (var path in candidatePaths)
        {
            if (File.Exists(path))
                return path;
        }

        // Try registry lookup on Windows
        if (OperatingSystem.IsWindows())
        {
            try
            {
                var regPath = GetExeFromRegistry();
                if (!string.IsNullOrEmpty(regPath) && File.Exists(regPath))
                    return regPath;
            }
            catch
            {
                // Registry read fallback
            }
        }

        return null;
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
                // Format: "C:\path\to\osu!.exe" "%1"
                var parts = val.Split('"');
                if (parts.Length > 1 && File.Exists(parts[1]))
                    return parts[1];
            }
        }
        return null;
    }

    private static string? FindDataDirectory()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var defaultDataDir = Path.Combine(appData, "osu");

        if (Directory.Exists(defaultDataDir))
            return defaultDataDir;

        return null;
    }
}
