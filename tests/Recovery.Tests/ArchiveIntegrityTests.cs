using System.IO.Compression;
using System.Text;
using Recovery.Downloads;
using Xunit;

namespace Recovery.Tests;

public class ArchiveIntegrityTests : IDisposable
{
    private readonly string _testDir;

    public ArchiveIntegrityTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "Recovery_ArchiveTests_" + Guid.NewGuid().ToString("N"));
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
    public async Task ValidateOsz_HtmlContent_IsRejected()
    {
        var filePath = Path.Combine(_testDir, "error.osz");
        await File.WriteAllTextAsync(filePath, "<!DOCTYPE html><html><head><title>404 Not Found</title></head><body>Beatmap not found</body></html>");

        var result = await ArchiveValidator.ValidateOszAsync(filePath);

        Assert.False(result.IsValid);
        Assert.Contains("HTML error page", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateOsz_EmptyFile_IsRejected()
    {
        var filePath = Path.Combine(_testDir, "empty.osz");
        await File.WriteAllBytesAsync(filePath, Array.Empty<byte>());

        var result = await ArchiveValidator.ValidateOszAsync(filePath);

        Assert.False(result.IsValid);
        Assert.Contains("empty", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateOsz_ZipWithoutOsuFiles_IsRejected()
    {
        var filePath = Path.Combine(_testDir, "no_osu.osz");
        using (var archive = ZipFile.Open(filePath, ZipArchiveMode.Create))
        {
            var entry = archive.CreateEntry("song.mp3");
            using var stream = entry.Open();
            stream.Write(new byte[] { 1, 2, 3, 4 });
        }

        var result = await ArchiveValidator.ValidateOszAsync(filePath);

        Assert.False(result.IsValid);
        Assert.Contains("contains no .osu beatmap files", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateOsz_ValidOsz_IsAcceptedWithSha256()
    {
        var filePath = Path.Combine(_testDir, "valid.osz");
        using (var archive = ZipFile.Open(filePath, ZipArchiveMode.Create))
        {
            var osuEntry = archive.CreateEntry("beatmap.osu");
            using (var stream = osuEntry.Open())
            using (var writer = new StreamWriter(stream, Encoding.UTF8))
            {
                writer.WriteLine("osu file format v14");
                writer.WriteLine("[General]");
                writer.WriteLine("AudioFilename: audio.mp3");
            }

            var audioEntry = archive.CreateEntry("audio.mp3");
            using (var stream = audioEntry.Open())
            {
                stream.Write(new byte[] { 0xFF, 0xFB, 0x90, 0x64 });
            }
        }

        var result = await ArchiveValidator.ValidateOszAsync(filePath);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.NotNull(result.Sha256Hash);
        Assert.Equal(64, result.Sha256Hash.Length);
    }
}
