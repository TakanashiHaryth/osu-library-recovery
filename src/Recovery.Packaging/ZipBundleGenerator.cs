using System.IO.Compression;
using System.Text;
using Recovery.Core.Models;

namespace Recovery.Packaging;

public class ZipBundleGenerator
{
    /// <summary>
    /// Builds the portable ZIP recovery bundle according to PRD Section 10.1.
    /// Stores each .osz separately inside the 'beatmaps/' folder without recompressing,
    /// along with manifest.json, manifest.csv, checksums.sha256, and recovery-report.txt.
    /// </summary>
    public static async Task BuildBundleAsync(
        string targetZipPath,
        string username,
        List<(BeatmapSet Set, string LocalOszPath)> packages,
        List<BeatmapSet> allDiscoveredSets,
        CancellationToken ct = default)
    {
        var tempZip = targetZipPath + ".tmp";
        if (File.Exists(tempZip))
            File.Delete(tempZip);

        using (var archive = ZipFile.Open(tempZip, ZipArchiveMode.Create))
        {
            var checksumSb = new StringBuilder();

            // 1. Add all .osz packages under "beatmaps/"
            foreach (var (set, localPath) in packages)
            {
                if (File.Exists(localPath))
                {
                    var entryName = $"beatmaps/{set.GetSafePackageFileName()}";
                    // Add .osz as Store (no recompression) to maximize speed and integrity
                    archive.CreateEntryFromFile(localPath, entryName, CompressionLevel.NoCompression);

                    if (!string.IsNullOrEmpty(set.Sha256Hash))
                    {
                        checksumSb.AppendLine($"{set.Sha256Hash}  {entryName}");
                    }
                }
            }

            // 2. Add manifest.json
            var jsonContent = ManifestGenerator.GenerateJsonManifest(allDiscoveredSets, username);
            await AddTextEntryAsync(archive, "manifest.json", jsonContent, ct);

            // 3. Add manifest.csv
            var csvContent = ManifestGenerator.GenerateCsvManifest(allDiscoveredSets);
            await AddTextEntryAsync(archive, "manifest.csv", csvContent, ct);

            // 4. Add checksums.sha256
            await AddTextEntryAsync(archive, "checksums.sha256", checksumSb.ToString(), ct);

            // 5. Add recovery-report.txt
            var reportContent = ManifestGenerator.GenerateReport(allDiscoveredSets, username);
            await AddTextEntryAsync(archive, "recovery-report.txt", reportContent, ct);
        }

        if (File.Exists(targetZipPath))
            File.Delete(targetZipPath);

        File.Move(tempZip, targetZipPath);
    }

    private static async Task AddTextEntryAsync(ZipArchive archive, string entryName, string text, CancellationToken ct)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(text.AsMemory(), ct);
    }
}
