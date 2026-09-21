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
    public static LazerInstallation Detect(string? customDataDir = null)
    {
        string? exePath = FindExecutable();
        string? dataDir = FindDataDirectory(customDataDir);

        return new LazerInstallation(
            exePath,
            dataDir,
            (!string.IsNullOrEmpty(exePath) && File.Exists(exePath)) || !string.IsNullOrEmpty(dataDir)
        );
    }

    private static string? FindExecutable()
    {
        if (OperatingSystem.IsWindows())
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
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
        else if (OperatingSystem.IsLinux())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var linuxCandidates = new[]
            {
                // Native packages (Arch, Debian/Ubuntu, Fedora)
                "/usr/bin/osu-lazer",
                "/usr/local/bin/osu-lazer",
                "/usr/bin/osu!",
                // Flatpak exports
                Path.Combine(home, ".local", "share", "flatpak", "exports", "bin", "sh.ppy.osu"),
                "/var/lib/flatpak/exports/bin/sh.ppy.osu",
                // AppImages
                Path.Combine(home, "Applications", "osu.AppImage"),
                Path.Combine(home, ".local", "bin", "osu.AppImage"),
                Path.Combine(home, "osu.AppImage")
            };

            foreach (var path in linuxCandidates)
            {
                if (File.Exists(path))
                    return path;
            }

            // Check PATH environment variable for osu-lazer or sh.ppy.osu
            var pathVar = Environment.GetEnvironmentVariable("PATH");
            if (!string.IsNullOrEmpty(pathVar))
            {
                var pathDirs = pathVar.Split(Path.PathSeparator);
                var binaryNames = new[] { "osu-lazer", "osu!", "sh.ppy.osu" };
                foreach (var dir in pathDirs)
                {
                    foreach (var bin in binaryNames)
                    {
                        var candidate = Path.Combine(dir, bin);
                        if (File.Exists(candidate))
                            return candidate;
                    }
                }
            }
        }
        else if (OperatingSystem.IsMacOS())
        {
            var macCandidate = "/Applications/osu!.app/Contents/MacOS/osu!";
            if (File.Exists(macCandidate))
                return macCandidate;
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

    private static string? FindDataDirectory(string? customDataDir = null)
    {
        if (!string.IsNullOrEmpty(customDataDir) && Directory.Exists(customDataDir))
            return customDataDir;

        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var defaultDataDir = Path.Combine(appData, "osu");
            if (Directory.Exists(defaultDataDir))
                return defaultDataDir;
        }
        else if (OperatingSystem.IsMacOS())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var macDataDir = Path.Combine(home, "Library", "Application Support", "osu");
            if (Directory.Exists(macDataDir))
                return macDataDir;
        }
        else
        {
            // Linux: Flatpak or native ~/.local/share/osu
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var flatpakDir = Path.Combine(home, ".var", "app", "sh.ppy.osu", "data", "osu");
            if (Directory.Exists(flatpakDir))
                return flatpakDir;

            var nativeDir = Path.Combine(home, ".local", "share", "osu");
            if (Directory.Exists(nativeDir))
                return nativeDir;
        }

        return null;
    }
}
