using System.Diagnostics;

namespace Recovery.Import;

/// <summary>
/// Handles direct and safe package import into osu! (Stable).
/// Automatically places validated .osz archives into the Songs folder and optionally invokes the running client.
/// </summary>
public class StableImportHandoff : IClientImportHandoff
{
    private readonly StableInstallation _installation;

    public StableImportHandoff(StableInstallation installation)
    {
        _installation = installation;
    }

    public async Task<ImportResult> HandoffOszAsync(string oszFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(oszFilePath))
            return new ImportResult(false, "Package file does not exist.", false);

        var fileName = Path.GetFileName(oszFilePath);

        // 1. If Songs folder is available, copy directly into Songs/
        // osu! Stable's FileSystemWatcher automatically unzips and registers .osz files placed in Songs/!
        if (!string.IsNullOrEmpty(_installation.SongsDirectory))
        {
            try
            {
                if (!Directory.Exists(_installation.SongsDirectory))
                {
                    Directory.CreateDirectory(_installation.SongsDirectory);
                }

                var targetPath = Path.Combine(_installation.SongsDirectory, fileName);
                File.Copy(oszFilePath, targetPath, overwrite: true);

                // If osu!.exe process is actively running, trigger osu! to reload/extract immediately
                if (!string.IsNullOrEmpty(_installation.ExecutablePath) && File.Exists(_installation.ExecutablePath))
                {
                    try
                    {
                        var runningOsu = Process.GetProcessesByName("osu!");
                        if (runningOsu.Length > 0)
                        {
                            bool useWine = OperatingSystem.IsLinux() && _installation.ExecutablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
                            var psi = new ProcessStartInfo
                            {
                                FileName = useWine ? "wine" : _installation.ExecutablePath,
                                Arguments = useWine ? $"\"{_installation.ExecutablePath}\" \"{targetPath}\"" : $"\"{targetPath}\"",
                                UseShellExecute = false,
                                CreateNoWindow = true
                            };
                            using var p = Process.Start(psi);
                            await Task.Delay(250, ct);
                        }
                    }
                    catch
                    {
                        // Background signal is best-effort; file is already in Songs/
                    }
                }

                return new ImportResult(true, $"Imported to osu! Stable Songs: {fileName}", true);
            }
            catch (Exception ex)
            {
                return new ImportResult(false, $"Failed copying package to Songs directory: {ex.Message}", false);
            }
        }

        // 2. Fallback: launch via osu!.exe
        if (!string.IsNullOrEmpty(_installation.ExecutablePath) && File.Exists(_installation.ExecutablePath))
        {
            try
            {
                bool useWine = OperatingSystem.IsLinux() && _installation.ExecutablePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
                var psi = new ProcessStartInfo
                {
                    FileName = useWine ? "wine" : _installation.ExecutablePath,
                    Arguments = useWine ? $"\"{_installation.ExecutablePath}\" \"{oszFilePath}\"" : $"\"{oszFilePath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                await Task.Delay(350, ct);
                return new ImportResult(true, "Handed off to osu!.exe process.", true);
            }
            catch (Exception ex)
            {
                return new ImportResult(false, $"Failed to launch osu!.exe: {ex.Message}", false);
            }
        }

        // 3. Fallback: OS file association
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = oszFilePath,
                UseShellExecute = true
            };
            Process.Start(psi);
            return new ImportResult(true, "Handed off to default OS file association.", true);
        }
        catch (Exception ex)
        {
            return new ImportResult(false, $"Failed to open package: {ex.Message}", false);
        }
    }
}
