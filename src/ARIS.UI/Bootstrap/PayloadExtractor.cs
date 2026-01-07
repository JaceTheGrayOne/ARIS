using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text.Json;

namespace ARIS.UI.Bootstrap;

/// <summary>
/// Extracts the embedded payload to the user's local application data folder.
/// Uses SHA-256 hashing and lock files for idempotent extraction.
/// </summary>
public sealed class PayloadExtractor
{
    private const string PayloadResourceName = "ARIS.Payload";
    private const string LockFileName = ".payload.lock";

    private readonly string _extractionRoot;

    /// <summary>
    /// Gets the path to the extracted payload directory.
    /// </summary>
    public string PayloadPath { get; private set; } = string.Empty;

    public PayloadExtractor()
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _extractionRoot = Path.Combine(localAppData, "ARIS", "payload");
    }

    /// <summary>
    /// Extracts the payload if needed and returns the path to the hosting executable.
    /// </summary>
    /// <returns>Path to Aris.Hosting.exe in the extracted payload.</returns>
    public async Task<string> ExtractAsync(CancellationToken cancellationToken = default)
    {
        // Get payload stream from embedded resources
        var assembly = Assembly.GetExecutingAssembly();
        using var payloadStream = assembly.GetManifestResourceStream(PayloadResourceName);

        if (payloadStream == null)
        {
            throw new PayloadNotFoundException();
        }

        // Compute payload hash
        var payloadHash = await ComputePayloadHashAsync(payloadStream, cancellationToken);
        payloadStream.Position = 0; // Reset stream position after hashing

        // Determine extraction directory based on hash
        var payloadId = payloadHash[..16]; // First 16 chars of hash
        PayloadPath = Path.Combine(_extractionRoot, payloadId);

        // Check if extraction is up-to-date
        if (IsExtractionUpToDate(payloadHash))
        {
            return GetHostingExecutablePath();
        }

        // Ensure extraction directory exists
        if (!Directory.Exists(PayloadPath))
        {
            Directory.CreateDirectory(PayloadPath);
        }

        try
        {
            // Extract payload
            await ExtractPayloadAsync(payloadStream, cancellationToken);

            // Write lock file
            await WriteLockFileAsync(payloadHash, cancellationToken);

            return GetHostingExecutablePath();
        }
        catch (Exception ex) when (ex is not BootstrapException)
        {
            throw new PayloadExtractionException($"Failed to extract payload: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Computes SHA-256 hash of a stream.
    /// </summary>
    public static async Task<string> ComputePayloadHashAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Parses a lock file from JSON content.
    /// </summary>
    public static PayloadLockFile? ParseLockFile(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<PayloadLockFile>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if the payload hash matches the lock file.
    /// </summary>
    public bool IsPayloadUpToDate(string currentHash, PayloadLockFile? lockFile)
    {
        if (lockFile == null)
            return false;

        return string.Equals(lockFile.PayloadHash, currentHash, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsExtractionUpToDate(string currentHash)
    {
        var lockFilePath = Path.Combine(PayloadPath, LockFileName);
        if (!File.Exists(lockFilePath))
            return false;

        try
        {
            var json = File.ReadAllText(lockFilePath);
            var lockFile = ParseLockFile(json);
            return IsPayloadUpToDate(currentHash, lockFile);
        }
        catch
        {
            return false;
        }
    }

    private Task ExtractPayloadAsync(Stream payloadStream, CancellationToken cancellationToken)
    {
        // Clear existing extraction (if partial)
        if (Directory.Exists(PayloadPath))
        {
            var lockFilePath = Path.Combine(PayloadPath, LockFileName);
            foreach (var file in Directory.GetFiles(PayloadPath, "*", SearchOption.AllDirectories))
            {
                if (file != lockFilePath)
                {
                    File.Delete(file);
                }
            }
            foreach (var dir in Directory.GetDirectories(PayloadPath))
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        // Extract zip archive
        using var archive = new ZipArchive(payloadStream, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var targetPath = Path.Combine(PayloadPath, entry.FullName);
            var targetDir = Path.GetDirectoryName(targetPath);

            // Skip directory entries
            if (string.IsNullOrEmpty(entry.Name))
            {
                if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                {
                    Directory.CreateDirectory(targetDir);
                }
                continue;
            }

            // Ensure directory exists
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            // Extract file
            entry.ExtractToFile(targetPath, overwrite: true);
        }

        return Task.CompletedTask;
    }

    private async Task WriteLockFileAsync(string payloadHash, CancellationToken cancellationToken)
    {
        var lockFile = new PayloadLockFile
        {
            PayloadHash = payloadHash,
            ExtractedAt = DateTimeOffset.UtcNow,
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0"
        };

        var json = JsonSerializer.Serialize(lockFile, new JsonSerializerOptions { WriteIndented = true });
        var lockFilePath = Path.Combine(PayloadPath, LockFileName);
        await File.WriteAllTextAsync(lockFilePath, json, cancellationToken);
    }

    private string GetHostingExecutablePath()
    {
        return Path.Combine(PayloadPath, "Aris.Hosting.exe");
    }
}
