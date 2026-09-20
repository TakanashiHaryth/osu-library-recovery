using Recovery.Core.Models;

namespace Recovery.Downloads;

public record DownloadResult(bool Success, string? LocalPath, string? ErrorMessage, string? Sha256Hash, long FileSizeBytes);

public class BeatmapDownloader
{
    private readonly HttpClient _httpClient;
    private const string MirrorBaseUrl = "https://api.nerinyan.moe/d/";

    public BeatmapDownloader(HttpClient? httpClient = null)
    {
        if (httpClient != null)
        {
            _httpClient = httpClient;
        }
        else
        {
            var handler = new HttpClientHandler { AllowAutoRedirect = true };
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }
    }

    /// <summary>
    /// Downloads a beatmap set archive (.osz), validates its archive integrity and SHA-256,
    /// and saves it to the target file path.
    /// </summary>
    public virtual async Task<DownloadResult> DownloadSetAsync(
        BeatmapSet set,
        string targetFilePath,
        DownloadVariant variant,
        IProgress<double>? progress = null,
        CancellationToken ct = default)
    {
        string downloadUrl = $"{MirrorBaseUrl}{set.SetId}";
        if (variant == DownloadVariant.NoVideo)
        {
            downloadUrl += "?noVideo=true";
        }

        var tempFilePath = targetFilePath + ".tmp";

        try
        {
            var targetDir = Path.GetDirectoryName(targetFilePath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            // 1. Send HTTP GET request with streaming
            using var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, ct);
            if (!response.IsSuccessStatusCode)
            {
                return new DownloadResult(false, null, $"Server returned HTTP status {(int)response.StatusCode} ({response.ReasonPhrase})", null, 0);
            }

            var totalBytes = response.Content.Headers.ContentLength ?? -1L;

            // 2. Stream to temporary file
            using (var responseStream = await response.Content.ReadAsStreamAsync(ct))
            using (var fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true))
            {
                var buffer = new byte[81920];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await responseStream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    totalRead += bytesRead;

                    if (totalBytes > 0)
                    {
                        double pct = (double)totalRead / totalBytes * 100.0;
                        progress?.Report(pct);
                    }
                }
            }

            // 3. Validate archive integrity
            var validation = await ArchiveValidator.ValidateOszAsync(tempFilePath, ct);
            if (!validation.IsValid)
            {
                if (File.Exists(tempFilePath))
                    File.Delete(tempFilePath);

                return new DownloadResult(false, null, $"Validation failed: {validation.ErrorMessage}", null, validation.FileSizeBytes);
            }

            // 4. Move temp file to final target
            if (File.Exists(targetFilePath))
            {
                File.Delete(targetFilePath);
            }

            File.Move(tempFilePath, targetFilePath);

            // Update set model properties
            set.Sha256Hash = validation.Sha256Hash;
            set.FileSizeBytes = validation.FileSizeBytes;

            return new DownloadResult(true, targetFilePath, null, validation.Sha256Hash, validation.FileSizeBytes);
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);

            return new DownloadResult(false, null, "Download was cancelled.", null, 0);
        }
        catch (Exception ex)
        {
            if (File.Exists(tempFilePath))
                File.Delete(tempFilePath);

            return new DownloadResult(false, null, $"Download error: {ex.Message}", null, 0);
        }
    }
}
