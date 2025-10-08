using System.IO.Compression;
using Debeon.ThirdParty.Models;

namespace Debeon.ThirdParty.Services;

public class PackageExtractor
{
    private readonly string _tempDirectory;
    private readonly SemaphoreSlim _extractionLock;

    public event EventHandler<ExtractionProgress>? ProgressChanged;

    public PackageExtractor(string tempDirectory)
    {
        _tempDirectory = tempDirectory;
        _extractionLock = new SemaphoreSlim(2, 2);
    }

    public async Task<string> ExtractPackageAsync(string packagePath, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        await _extractionLock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(packagePath))
            {
                throw new FileNotFoundException($"Package not found: {packagePath}");
            }

            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            string extractionPath = destinationDirectory;

            if (packagePath.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractZipAsync(packagePath, extractionPath, cancellationToken);
            }
            else if (packagePath.EndsWith(".gz", StringComparison.OrdinalIgnoreCase))
            {
                await ExtractGzipAsync(packagePath, extractionPath, cancellationToken);
            }
            else
            {
                string fileName = Path.GetFileName(packagePath);
                string destPath = Path.Combine(extractionPath, fileName);
                File.Copy(packagePath, destPath, true);
            }

            return extractionPath;
        }
        finally
        {
            _extractionLock.Release();
        }
    }

    private async Task ExtractZipAsync(string zipPath, string destinationPath, CancellationToken cancellationToken)
    {
        using var archive = ZipFile.OpenRead(zipPath);
        int totalEntries = archive.Entries.Count;
        int extractedEntries = 0;

        foreach (var entry in archive.Entries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            string destinationFilePath = Path.Combine(destinationPath, entry.FullName);
            string? directory = Path.GetDirectoryName(destinationFilePath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!string.IsNullOrEmpty(entry.Name))
            {
                entry.ExtractToFile(destinationFilePath, true);
            }

            extractedEntries++;

            ProgressChanged?.Invoke(this, new ExtractionProgress
            {
                PackagePath = zipPath,
                TotalFiles = totalEntries,
                ExtractedFiles = extractedEntries,
                Percentage = (extractedEntries * 100.0) / totalEntries,
                CurrentFile = entry.FullName
            });
        }

        await Task.CompletedTask;
    }

    private async Task ExtractGzipAsync(string gzipPath, string destinationPath, CancellationToken cancellationToken)
    {
        string outputFileName = Path.GetFileNameWithoutExtension(gzipPath);
        string outputPath = Path.Combine(destinationPath, outputFileName);

        using var inputStream = File.OpenRead(gzipPath);
        using var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = File.Create(outputPath);

        var buffer = new byte[8192];
        int bytesRead;
        long totalBytesRead = 0;
        long inputFileSize = inputStream.Length;

        while ((bytesRead = await gzipStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await outputStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalBytesRead += bytesRead;

            ProgressChanged?.Invoke(this, new ExtractionProgress
            {
                PackagePath = gzipPath,
                TotalFiles = 1,
                ExtractedFiles = 0,
                Percentage = (totalBytesRead * 100.0) / inputFileSize,
                CurrentFile = outputFileName
            });
        }

        ProgressChanged?.Invoke(this, new ExtractionProgress
        {
            PackagePath = gzipPath,
            TotalFiles = 1,
            ExtractedFiles = 1,
            Percentage = 100,
            CurrentFile = outputFileName
        });
    }

    public async Task<List<string>> ExtractMultiplePackagesAsync(List<string> packagePaths, string destinationDirectory, CancellationToken cancellationToken = default)
    {
        var extractedPaths = new List<string>();

        foreach (var packagePath in packagePaths)
        {
            var extractedPath = await ExtractPackageAsync(packagePath, destinationDirectory, cancellationToken);
            extractedPaths.Add(extractedPath);
        }

        return extractedPaths;
    }

    public async Task<bool> ValidateExtractedFilesAsync(string directory, List<FileEntry> expectedFiles, CancellationToken cancellationToken = default)
    {
        foreach (var fileEntry in expectedFiles)
        {
            string filePath = Path.Combine(directory, fileEntry.RelativePath);

            if (!File.Exists(filePath))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(fileEntry.Hash))
            {
                string actualHash = await ComputeFileHashAsync(filePath, cancellationToken);
                if (!actualHash.Equals(fileEntry.Hash, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            var fileInfo = new FileInfo(filePath);
            if (fileEntry.Size > 0 && fileInfo.Length != fileEntry.Size)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await md5.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }
}

public class ExtractionProgress
{
    public string PackagePath { get; set; } = string.Empty;
    public int TotalFiles { get; set; }
    public int ExtractedFiles { get; set; }
    public double Percentage { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
}
