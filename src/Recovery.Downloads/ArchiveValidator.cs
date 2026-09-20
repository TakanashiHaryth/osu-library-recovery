using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Recovery.Downloads;

public record ValidationResult(bool IsValid, string? ErrorMessage, string? Sha256Hash, long FileSizeBytes);

public class ArchiveValidator
{
    private static readonly byte[] ZipMagicHeader = new byte[] { 0x50, 0x4B, 0x03, 0x04 }; // "PK\x03\x04"

    /// <summary>
    /// Validates an .osz package: verifies file size, magic header, HTML rejection,
    /// ZIP integrity, presence of .osu beatmap files, and calculates SHA-256 hash.
    /// </summary>
    public static async Task<ValidationResult> ValidateOszAsync(string filePath, CancellationToken ct = default)
    {
        if (!File.Exists(filePath))
            return new ValidationResult(false, "File does not exist.", null, 0);

        var fileInfo = new FileInfo(filePath);
        if (fileInfo.Length == 0)
            return new ValidationResult(false, "File is empty (0 bytes).", null, 0);

        // Check for HTML error response
        using (var stream = File.OpenRead(filePath))
        {
            var headerBytes = new byte[Math.Min(512, (int)stream.Length)];
            var read = await stream.ReadAsync(headerBytes.AsMemory(0, headerBytes.Length), ct);
            
            var text = Encoding.UTF8.GetString(headerBytes, 0, read).TrimStart();
            if (text.StartsWith("<!DOCTYPE html", StringComparison.OrdinalIgnoreCase) ||
                text.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("<head>", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult(false, "Response is an HTML error page or login redirect, not a valid .osz archive.", null, fileInfo.Length);
            }

            // Check ZIP magic header
            if (headerBytes.Length < 4 ||
                headerBytes[0] != ZipMagicHeader[0] ||
                headerBytes[1] != ZipMagicHeader[1] ||
                headerBytes[2] != ZipMagicHeader[2] ||
                headerBytes[3] != ZipMagicHeader[3])
            {
                return new ValidationResult(false, "File does not have a valid ZIP magic header.", null, fileInfo.Length);
            }
        }

        // Test opening the ZIP archive and verify it contains .osu files
        try
        {
            using (var zip = ZipFile.OpenRead(filePath))
            {
                bool hasOsuFile = zip.Entries.Any(e => e.FullName.EndsWith(".osu", StringComparison.OrdinalIgnoreCase));
                if (!hasOsuFile)
                {
                    return new ValidationResult(false, "Archive is a ZIP but contains no .osu beatmap files.", null, fileInfo.Length);
                }
            }
        }
        catch (InvalidDataException ex)
        {
            return new ValidationResult(false, $"Archive is corrupted or truncated: {ex.Message}", null, fileInfo.Length);
        }
        catch (Exception ex)
        {
            return new ValidationResult(false, $"Failed to read archive: {ex.Message}", null, fileInfo.Length);
        }

        // Calculate SHA-256
        string sha256;
        using (var stream = File.OpenRead(filePath))
        using (var sha = SHA256.Create())
        {
            var hashBytes = await sha.ComputeHashAsync(stream, ct);
            sha256 = Convert.ToHexStringLower(hashBytes);
        }

        return new ValidationResult(true, null, sha256, fileInfo.Length);
    }
}
