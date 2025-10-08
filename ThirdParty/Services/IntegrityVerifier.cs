using System.Security.Cryptography;
using Debeon.ThirdParty.Models;

namespace Debeon.ThirdParty.Services;

public class IntegrityVerifier
{
    private readonly SemaphoreSlim _verificationLock;

    public event EventHandler<VerificationProgress>? ProgressChanged;

    public IntegrityVerifier()
    {
        _verificationLock = new SemaphoreSlim(4, 4);
    }

    public async Task<VerificationResult> VerifyFileAsync(string filePath, string expectedHash, HashAlgorithmType algorithmType = HashAlgorithmType.MD5, CancellationToken cancellationToken = default)
    {
        await _verificationLock.WaitAsync(cancellationToken);

        try
        {
            if (!File.Exists(filePath))
            {
                return new VerificationResult
                {
                    FilePath = filePath,
                    IsValid = false,
                    ErrorMessage = "File not found"
                };
            }

            string actualHash = await ComputeFileHashAsync(filePath, algorithmType, cancellationToken);

            bool isValid = actualHash.Equals(expectedHash, StringComparison.OrdinalIgnoreCase);

            return new VerificationResult
            {
                FilePath = filePath,
                ExpectedHash = expectedHash,
                ActualHash = actualHash,
                IsValid = isValid,
                AlgorithmUsed = algorithmType,
                FileSize = new FileInfo(filePath).Length
            };
        }
        finally
        {
            _verificationLock.Release();
        }
    }

    public async Task<List<VerificationResult>> VerifyMultipleFilesAsync(Dictionary<string, string> fileHashPairs, HashAlgorithmType algorithmType = HashAlgorithmType.MD5, CancellationToken cancellationToken = default)
    {
        var results = new List<VerificationResult>();
        int totalFiles = fileHashPairs.Count;
        int verifiedFiles = 0;

        var tasks = fileHashPairs.Select(async pair =>
        {
            var result = await VerifyFileAsync(pair.Key, pair.Value, algorithmType, cancellationToken);

            verifiedFiles++;
            ProgressChanged?.Invoke(this, new VerificationProgress
            {
                TotalFiles = totalFiles,
                VerifiedFiles = verifiedFiles,
                Percentage = (verifiedFiles * 100.0) / totalFiles,
                CurrentFile = pair.Key
            });

            return result;
        });

        results = (await Task.WhenAll(tasks)).ToList();
        return results;
    }

    public async Task<bool> VerifyDeploymentIntegrityAsync(RobloxDeployment deployment, string installDirectory, CancellationToken cancellationToken = default)
    {
        var fileHashPairs = new Dictionary<string, string>();

        foreach (var package in deployment.Packages)
        {
            string filePath = Path.Combine(installDirectory, package.Key);
            fileHashPairs[filePath] = package.Value.Checksum;
        }

        var results = await VerifyMultipleFilesAsync(fileHashPairs, HashAlgorithmType.MD5, cancellationToken);
        return results.All(r => r.IsValid);
    }

    private async Task<string> ComputeFileHashAsync(string filePath, HashAlgorithmType algorithmType, CancellationToken cancellationToken)
    {
        using HashAlgorithm hashAlgorithm = algorithmType switch
        {
            HashAlgorithmType.MD5 => MD5.Create(),
            HashAlgorithmType.SHA1 => SHA1.Create(),
            HashAlgorithmType.SHA256 => SHA256.Create(),
            HashAlgorithmType.SHA512 => SHA512.Create(),
            _ => MD5.Create()
        };

        using var stream = File.OpenRead(filePath);
        var hashBytes = await hashAlgorithm.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public async Task<bool> VerifyDirectoryIntegrityAsync(string directory, List<FileEntry> expectedFiles, CancellationToken cancellationToken = default)
    {
        var fileHashPairs = new Dictionary<string, string>();

        foreach (var fileEntry in expectedFiles)
        {
            string filePath = Path.Combine(directory, fileEntry.RelativePath);
            if (!string.IsNullOrEmpty(fileEntry.Hash))
            {
                fileHashPairs[filePath] = fileEntry.Hash;
            }
        }

        var results = await VerifyMultipleFilesAsync(fileHashPairs, HashAlgorithmType.MD5, cancellationToken);
        return results.All(r => r.IsValid);
    }

    public async Task<QuarantineDecision> AnalyzeFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return new QuarantineDecision
            {
                ShouldQuarantine = true,
                Reason = "File not found",
                Severity = QuarantineSeverity.High
            };
        }

        var fileInfo = new FileInfo(filePath);
        var decision = new QuarantineDecision
        {
            FilePath = filePath,
            FileSize = fileInfo.Length
        };

        if (fileInfo.Length == 0)
        {
            decision.ShouldQuarantine = true;
            decision.Reason = "File is empty";
            decision.Severity = QuarantineSeverity.Medium;
            return decision;
        }

        string hash = await ComputeFileHashAsync(filePath, HashAlgorithmType.SHA256, cancellationToken);
        decision.FileHash = hash;

        decision.ShouldQuarantine = false;
        decision.Reason = "File appears valid";
        decision.Severity = QuarantineSeverity.None;

        return decision;
    }
}

public class VerificationResult
{
    public string FilePath { get; set; } = string.Empty;
    public string ExpectedHash { get; set; } = string.Empty;
    public string ActualHash { get; set; } = string.Empty;
    public bool IsValid { get; set; }
    public HashAlgorithmType AlgorithmUsed { get; set; }
    public long FileSize { get; set; }
    public string? ErrorMessage { get; set; }
}

public class VerificationProgress
{
    public int TotalFiles { get; set; }
    public int VerifiedFiles { get; set; }
    public double Percentage { get; set; }
    public string CurrentFile { get; set; } = string.Empty;
}

public enum HashAlgorithmType
{
    MD5,
    SHA1,
    SHA256,
    SHA512
}

public class QuarantineDecision
{
    public string FilePath { get; set; } = string.Empty;
    public bool ShouldQuarantine { get; set; }
    public string Reason { get; set; } = string.Empty;
    public QuarantineSeverity Severity { get; set; }
    public long FileSize { get; set; }
    public string? FileHash { get; set; }
}

public enum QuarantineSeverity
{
    None,
    Low,
    Medium,
    High,
    Critical
}
