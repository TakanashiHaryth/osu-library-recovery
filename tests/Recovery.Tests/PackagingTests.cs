using System.IO.Compression;
using System.Text;
using Recovery.Core.Models;
using Recovery.Packaging;
using Xunit;

namespace Recovery.Tests;

public class PackagingTests : IDisposable
{
    private readonly string _testDir;

    public PackagingTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "Recovery_PackagingTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    [Fact]
    public async Task BuildBundleAsync_CreatesOuterZipWithAllPrdRequiredEntries()
    {
        // Arrange
        var mockOszPath = Path.Combine(_testDir, "temp_song.osz");
        using (var zip = ZipFile.Open(mockOszPath, ZipArchiveMode.Create))
        {
            var entry = zip.CreateEntry("song.osu");
            using var s = entry.Open();
            s.Write(Encoding.UTF8.GetBytes("osu file format v14"));
        }

        var set = new BeatmapSet
        {
            SetId = 1234,
            Artist = "Camellia",
            Title = "GHOST",
            Mapper = "MapperName",
            Status = "ranked",
            Sha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            RecoveryStatus = RecoveryItemStatus.Completed
        };

        var packages = new List<(BeatmapSet Set, string LocalOszPath)> { (set, mockOszPath) };
        var allSets = new List<BeatmapSet> { set };
        var targetZip = Path.Combine(_testDir, "recovery_bundle.zip");

        // Act
        await ZipBundleGenerator.BuildBundleAsync(targetZip, "TestPlayer", packages, allSets);

        // Assert
        Assert.True(File.Exists(targetZip));

        using var bundle = ZipFile.OpenRead(targetZip);
        var entryNames = bundle.Entries.Select(e => e.FullName).ToList();

        // Check required PRD Section 10.1 items
        Assert.Contains("manifest.json", entryNames);
        Assert.Contains("manifest.csv", entryNames);
        Assert.Contains("checksums.sha256", entryNames);
        Assert.Contains("recovery-report.txt", entryNames);
        Assert.Contains("beatmaps/1234 Camellia - GHOST.osz", entryNames);

        // Check checksums content
        var checksumEntry = bundle.GetEntry("checksums.sha256");
        Assert.NotNull(checksumEntry);
        using var stream = checksumEntry.Open();
        using var reader = new StreamReader(stream);
        var checksumText = await reader.ReadToEndAsync();
        Assert.Contains("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855  beatmaps/1234 Camellia - GHOST.osz", checksumText);
    }
}
