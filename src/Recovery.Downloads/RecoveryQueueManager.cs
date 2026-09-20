using Recovery.Core.Models;
using Recovery.Import;
using Recovery.Packaging;

namespace Recovery.Downloads;

public record QueueProgress(
    int CompletedCount,
    int TotalCount,
    double Percentage,
    string CurrentItemTitle,
    string StatusMessage
);

public class RecoveryQueueManager
{
    private readonly BeatmapDownloader _downloader;
    private readonly int _concurrency;

    public RecoveryQueueManager(BeatmapDownloader? downloader = null, int concurrency = 3)
    {
        _downloader = downloader ?? new BeatmapDownloader();
        _concurrency = Math.Max(1, concurrency);
    }

    public async Task RunRecoveryAsync(
        List<BeatmapSet> selectedSets,
        OutputMode mode,
        string destinationPath,
        string username,
        DownloadVariant defaultVariant = DownloadVariant.Full,
        IProgress<QueueProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (selectedSets.Count == 0)
        {
            progress?.Report(new QueueProgress(0, 0, 100, "", "No beatmap sets selected for recovery."));
            return;
        }

        int total = selectedSets.Count;
        int completed = 0;
        var downloadedPackages = new List<(BeatmapSet Set, string LocalPath)>();
        var lockObj = new object();

        // Determine working staging or destination folder
        string stagingDir = mode switch
        {
            OutputMode.DownloadFolder => destinationPath,
            _ => Path.Combine(Path.GetTempPath(), "RecoveryStaging_" + Guid.NewGuid().ToString("N"))
        };

        if (!Directory.Exists(stagingDir))
        {
            Directory.CreateDirectory(stagingDir);
        }

        // Initialize import handoff if AutoImport mode
        LazerImportHandoff? handoff = null;
        if (mode == OutputMode.AutoImport)
        {
            var install = LazerPathDetector.Detect();
            handoff = new LazerImportHandoff(install);
        }

        using var semaphore = new SemaphoreSlim(_concurrency, _concurrency);
        var tasks = new List<Task>();

        foreach (var set in selectedSets)
        {
            if (ct.IsCancellationRequested)
                break;

            await semaphore.WaitAsync(ct);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    set.RecoveryStatus = RecoveryItemStatus.Downloading;
                    var variant = set.RequestedVariant != DownloadVariant.Full ? set.RequestedVariant : defaultVariant;
                    var fileName = set.GetSafePackageFileName();
                    var targetFile = Path.Combine(stagingDir, fileName);

                    lock (lockObj)
                    {
                        progress?.Report(new QueueProgress(
                            completed, total, (double)completed / total * 100.0,
                            $"{set.Artist} - {set.Title}",
                            $"Downloading ({completed + 1}/{total}): {set.Title}..."
                        ));
                    }

                    var result = await _downloader.DownloadSetAsync(set, targetFile, variant, null, ct);

                    if (result.Success && !string.IsNullOrEmpty(result.LocalPath))
                    {
                        set.RecoveryStatus = RecoveryItemStatus.Completed;
                        set.Sha256Hash = result.Sha256Hash;
                        set.FileSizeBytes = result.FileSizeBytes;

                        lock (lockObj)
                        {
                            downloadedPackages.Add((set, result.LocalPath));
                        }

                        // If AutoImport, hand off to lazer client immediately
                        if (mode == OutputMode.AutoImport && handoff != null)
                        {
                            set.RecoveryStatus = RecoveryItemStatus.Importing;
                            var importRes = await handoff.HandoffOszAsync(result.LocalPath, ct);
                            if (importRes.Success)
                            {
                                set.RecoveryStatus = RecoveryItemStatus.Completed;
                            }
                            else
                            {
                                set.RecoveryStatus = RecoveryItemStatus.Failed;
                                set.FailureReason = importRes.Message;
                            }
                        }
                    }
                    else
                    {
                        set.RecoveryStatus = RecoveryItemStatus.Failed;
                        set.FailureReason = result.ErrorMessage;
                    }
                }
                catch (OperationCanceledException)
                {
                    set.RecoveryStatus = RecoveryItemStatus.Cancelled;
                    set.FailureReason = "Cancelled by user.";
                }
                catch (Exception ex)
                {
                    set.RecoveryStatus = RecoveryItemStatus.Failed;
                    set.FailureReason = ex.Message;
                }
                finally
                {
                    Interlocked.Increment(ref completed);
                    lock (lockObj)
                    {
                        double pct = (double)completed / total * 100.0;
                        progress?.Report(new QueueProgress(
                            completed, total, pct,
                            $"{set.Artist} - {set.Title}",
                            $"Processed ({completed}/{total}): {set.Title}"
                        ));
                    }
                    semaphore.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);

        // Final Packaging step if ZipBundle mode
        if (mode == OutputMode.ZipBundle && !ct.IsCancellationRequested)
        {
            progress?.Report(new QueueProgress(
                completed, total, 95.0,
                "Building ZIP Bundle",
                "Packaging .osz files and manifests into final ZIP archive..."
            ));

            await ZipBundleGenerator.BuildBundleAsync(destinationPath, username, downloadedPackages, selectedSets, ct);

            // Clean up temporary staging directory
            try
            {
                if (Directory.Exists(stagingDir))
                    Directory.Delete(stagingDir, true);
            }
            catch { }
        }

        progress?.Report(new QueueProgress(
            completed, total, 100.0,
            "Done",
            $"Recovery complete: {downloadedPackages.Count} sets restored successfully."
        ));
    }
}
