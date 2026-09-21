using System.Diagnostics;
using Recovery.Core.Models;

namespace Recovery.Import;

public record ImportResult(bool Success, string? Message, bool ClientInvoked);

public class LazerImportHandoff : IClientImportHandoff
{
    private readonly LazerInstallation _installation;

    public LazerImportHandoff(LazerInstallation installation)
    {
        _installation = installation;
    }

    /// <summary>
    /// Verifies that a staging path does NOT overlap the live osu!lazer database or files directory.
    /// PRD Section 16.1 Safety Rule.
    /// </summary>
    public bool IsPathSafeFromLiveStore(string targetPath)
    {
        if (string.IsNullOrEmpty(_installation.DataDirectory))
            return true;

        var fullTarget = Path.GetFullPath(targetPath);
        var liveData = Path.GetFullPath(_installation.DataDirectory);
        var liveRealm = Path.Combine(liveData, "client.realm");
        var liveFiles = Path.Combine(liveData, "files");

        if (fullTarget.Equals(liveRealm, StringComparison.OrdinalIgnoreCase) ||
            fullTarget.StartsWith(liveFiles, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Executes the safe lazer client handoff for a validated .osz package.
    /// Never writes directly to client.realm or files/.
    /// </summary>
    public async Task<ImportResult> HandoffOszAsync(string oszFilePath, CancellationToken ct = default)
    {
        if (!File.Exists(oszFilePath))
            return new ImportResult(false, "Package file does not exist.", false);

        if (!IsPathSafeFromLiveStore(oszFilePath))
            return new ImportResult(false, "Safety violation: Target file path overlaps live osu!lazer database or files store.", false);

        if (!_installation.IsInstalled || string.IsNullOrEmpty(_installation.ExecutablePath))
        {
            // Fallback: Use OS shell open if lazer executable path is not resolved explicitly
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
                return new ImportResult(false, $"Failed to launch default file association: {ex.Message}", false);
            }
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = _installation.ExecutablePath,
                Arguments = $"\"{oszFilePath}\"",
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
                return new ImportResult(false, "Failed to start osu!lazer process.", false);

            // Give process brief moment to initialize IPC connection to primary lazer instance
            await Task.Delay(500, ct);

            return new ImportResult(true, "Successfully passed package to osu!lazer client IPC.", true);
        }
        catch (Exception ex)
        {
            return new ImportResult(false, $"Error during osu!lazer handoff: {ex.Message}", false);
        }
    }
}
