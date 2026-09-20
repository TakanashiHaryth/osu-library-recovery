using System.IO.Compression;
using System.Text;
using Recovery.Core.Models;
using Recovery.Downloads;
using Xunit;

namespace Recovery.Tests;

public class RecoveryQueueTests : IDisposable
{
    private readonly string _testDir;

    public RecoveryQueueTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "RecoveryQueueTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, true); } catch { }
        }
    }

    private class MockDownloader : BeatmapDownloader
    {
        public override async Task<DownloadResult> DownloadSetAsync(
            BeatmapSet set,
            string targetFilePath,
            DownloadVariant variant,
            IProgress<double>? progress = null,
            CancellationToken ct = default)
        {
            var dir = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            using (var zip = ZipFile.Open(targetFilePath, ZipArchiveMode.Create))
            {
                var entry = zip.CreateEntry("map.osu");
                using var s = entry.Open();
                s.Write(Encoding.UTF8.GetBytes("osu file format v14"));
            }

            var validation = await ArchiveValidator.ValidateOszAsync(targetFilePath, ct);
            return new DownloadResult(true, targetFilePath, null, validation.Sha256Hash, validation.FileSizeBytes);
        }
    }

    [Fact]
    public async Task RunRecoveryAsync_DownloadFolderMode_SavesAllOszToFolder()
    {
        var targetFolder = Path.Combine(_testDir, "OutputSongs");
        var queueManager = new RecoveryQueueManager(new MockDownloader(), concurrency: 2);

        var sets = new List<BeatmapSet>
        {
            new BeatmapSet { SetId = 10, Artist = "Artist1", Title = "Title1", IsSelected = true },
            new BeatmapSet { SetId = 20, Artist = "Artist2", Title = "Title2", IsSelected = true }
        };

        var progressList = new List<QueueProgress>();
        var progress = new Progress<QueueProgress>(p => progressList.Add(p));

        await queueManager.RunRecoveryAsync(sets, OutputMode.DownloadFolder, targetFolder, "TestUser", DownloadVariant.Full, progress);

        Assert.True(Directory.Exists(targetFolder));
        Assert.True(File.Exists(Path.Combine(targetFolder, sets[0].GetSafePackageFileName())));
        Assert.True(File.Exists(Path.Combine(targetFolder, sets[1].GetSafePackageFileName())));
        Assert.Equal(RecoveryItemStatus.Completed, sets[0].RecoveryStatus);
        Assert.Equal(RecoveryItemStatus.Completed, sets[1].RecoveryStatus);
    }

    [Fact]
    public async Task RunRecoveryAsync_ZipBundleMode_CreatesValidZipBundle()
    {
        var targetZip = Path.Combine(_testDir, "recovery_bundle.zip");
        var queueManager = new RecoveryQueueManager(new MockDownloader(), concurrency: 2);

        var sets = new List<BeatmapSet>
        {
            new BeatmapSet { SetId = 101, Artist = "ArtistA", Title = "TitleA", IsSelected = true },
            new BeatmapSet { SetId = 102, Artist = "ArtistB", Title = "TitleB", IsSelected = true }
        };

        await queueManager.RunRecoveryAsync(sets, OutputMode.ZipBundle, targetZip, "TestUser", DownloadVariant.Full);

        Assert.True(File.Exists(targetZip));

        using var archive = ZipFile.OpenRead(targetZip);
        var entries = archive.Entries.Select(e => e.FullName).ToList();
        Assert.Contains("manifest.json", entries);
        Assert.Contains("manifest.csv", entries);
        Assert.Contains("checksums.sha256", entries);
        Assert.Contains("recovery-report.txt", entries);
        Assert.Contains($"beatmaps/{sets[0].GetSafePackageFileName()}", entries);
        Assert.Contains($"beatmaps/{sets[1].GetSafePackageFileName()}", entries);
    }
}
