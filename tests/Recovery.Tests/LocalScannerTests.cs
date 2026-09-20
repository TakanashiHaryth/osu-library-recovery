using System.Text;
using Recovery.Core.Models;
using Recovery.Core.Services;
using Recovery.Local;

namespace Recovery.Tests;

public class LocalScannerTests : IDisposable
{
    private readonly string _testDir;

    public LocalScannerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "RecoveryTest_" + Guid.NewGuid().ToString("N"));
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
    public void StorageLocator_GetDefaultOsuDirectory_ReturnsNonEmptyPath()
    {
        var defaultPath = OsuStorageLocator.GetDefaultOsuDirectory();
        Assert.False(string.IsNullOrWhiteSpace(defaultPath));
        Assert.Contains("osu", defaultPath);
    }

    [Fact]
    public void StorageLocator_IsValidOsuStorageDirectory_RejectsNonexistentOrEmpty()
    {
        Assert.False(OsuStorageLocator.IsValidOsuStorageDirectory(""));
        Assert.False(OsuStorageLocator.IsValidOsuStorageDirectory(Path.Combine(_testDir, "nonexistent")));
        Assert.False(OsuStorageLocator.IsValidOsuStorageDirectory(_testDir)); // empty dir
    }

    [Fact]
    public async Task LocalLibraryScanner_FindsBeatmapsInFilesDirectory()
    {
        // Set up mock osu! storage structure
        var filesDir = Path.Combine(_testDir, "files", "1", "1c");
        Directory.CreateDirectory(filesDir);

        var mockOsuContent = @"osu file format v14

[General]
AudioFilename: audio.mp3
Mode: 0

[Metadata]
Title:Test Song
Artist:Test Artist
Creator:Test Mapper
Version:Insane
BeatmapID:99999
BeatmapSetID:123456

[Difficulty]
HPDrainRate:5
";
        var beatmapFilePath = Path.Combine(filesDir, "1c9f97cda3db2fc0f2c8a8bcc3d18db84ff99225e81aaf3381023cc89681637a");
        await File.WriteAllTextAsync(beatmapFilePath, mockOsuContent, Encoding.UTF8);

        // Also add a non-osu file (e.g. image or random file)
        var nonOsuFilePath = Path.Combine(filesDir, "randomfile");
        await File.WriteAllTextAsync(nonOsuFilePath, "PNG image bytes here", Encoding.UTF8);

        var scanner = new LocalLibraryScanner();
        var result = await scanner.ScanInstalledLibraryAsync(_testDir);

        Assert.Single(result.InstalledSetIds);
        Assert.Contains(123456, result.InstalledSetIds);
        Assert.Single(result.DiscoveredBeatmaps);

        var record = result.DiscoveredBeatmaps[0];
        Assert.Equal(123456, record.SetId);
        Assert.Equal(99999, record.BeatmapId);
        Assert.Equal("Test Song", record.Title);
        Assert.Equal("Test Artist", record.Artist);
        Assert.Equal("Insane", record.Version);
    }

    [Fact]
    public async Task LocalArtefactScanner_ExtractsSetIdsFromLogFiles()
    {
        var logsDir = Path.Combine(_testDir, "logs");
        Directory.CreateDirectory(logsDir);

        var logContent = @"2026-09-17 01:40:04 [verbose]: Fetching cache...
2026-09-17 01:42:56 [verbose]: Request to https://osu.ppy.sh/beatmapsets/888888 successfully completed!
2026-09-17 01:46:49 [verbose]: Downloading beatmapset: 999999
";
        await File.WriteAllTextAsync(Path.Combine(logsDir, "runtime.log"), logContent, Encoding.UTF8);

        var scanner = new LocalArtefactScanner();
        var discovered = await scanner.ScanLogsAsync(logsDir);

        Assert.Equal(2, discovered.Count);
        Assert.Contains(discovered, s => s.SetId == 888888);
        Assert.Contains(discovered, s => s.SetId == 999999);
        Assert.All(discovered, s => Assert.Equal(EvidenceLevel.ProbableLocal, s.OverallEvidenceLevel));
    }

    [Fact]
    public async Task LocalArtefactScanner_RestoresFromManifestJson()
    {
        var manifestPath = Path.Combine(_testDir, "manifest.json");
        var manifestContent = @"{
  ""requested_sets"": [
    {
      ""set_id"": 777777,
      ""artist"": ""Artist A"",
      ""title"": ""Title A"",
      ""mapper"": ""Mapper A"",
      ""status"": ""ranked""
    }
  ]
}";
        await File.WriteAllTextAsync(manifestPath, manifestContent, Encoding.UTF8);

        var scanner = new LocalArtefactScanner();
        var restored = await scanner.ScanManifestJsonAsync(manifestPath);

        Assert.Single(restored);
        Assert.Equal(777777, restored[0].SetId);
        Assert.Equal("Artist A", restored[0].Artist);
        Assert.Equal("Title A", restored[0].Title);
        Assert.Equal(EvidenceLevel.ConfirmedLocal, restored[0].OverallEvidenceLevel);
    }

    [Fact]
    public void BeatmapFilter_HideInstalled_FiltersOutInstalledSets()
    {
        var sets = new List<BeatmapSet>
        {
            new() { SetId = 1, Title = "Installed Song", IsInstalled = true },
            new() { SetId = 2, Title = "Missing Song", IsInstalled = false }
        };

        // When HideInstalled is false
        var all = BeatmapFilter.ApplyFilter(sets, new FilterCriteria(HideInstalled: false));
        Assert.Equal(2, all.Count);

        // When HideInstalled is true
        var missingOnly = BeatmapFilter.ApplyFilter(sets, new FilterCriteria(HideInstalled: true));
        Assert.Single(missingOnly);
        Assert.Equal(2, missingOnly[0].SetId);
        Assert.Equal("Missing Song", missingOnly[0].Title);
    }
}
