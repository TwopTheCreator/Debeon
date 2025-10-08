using System.Text.Json;
using Debeon.ThirdParty.Models;

namespace Debeon.ThirdParty.Core;

public class VersionManager
{
    private readonly RobloxUrlResolver _urlResolver;
    private readonly string _cacheDirectory;
    private readonly Dictionary<RobloxChannel, List<RobloxVersion>> _versionCache;
    private readonly SemaphoreSlim _cacheLock;
    private DateTime _lastCacheUpdate;
    private const int CacheExpirationMinutes = 30;

    public VersionManager(RobloxUrlResolver urlResolver, string cacheDirectory)
    {
        _urlResolver = urlResolver;
        _cacheDirectory = cacheDirectory;
        _versionCache = new Dictionary<RobloxChannel, List<RobloxVersion>>();
        _cacheLock = new SemaphoreSlim(1, 1);
        _lastCacheUpdate = DateTime.MinValue;

        EnsureCacheDirectoryExists();
    }

    private void EnsureCacheDirectoryExists()
    {
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<RobloxVersion> GetLatestVersionAsync(RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        string versionHash = await _urlResolver.GetLatestVersionHashAsync(channel, cancellationToken);
        var deployment = await _urlResolver.GetDeploymentManifestAsync(versionHash, channel, cancellationToken);

        var version = new RobloxVersion
        {
            VersionHash = versionHash,
            ClientVersion = versionHash,
            ReleaseDate = DateTime.UtcNow,
            Channel = channel,
            IsStudioVersion = channel == RobloxChannel.Studio || channel == RobloxChannel.StudioCanary,
            TotalSize = deployment.Packages.Sum(p => p.Value.Size)
        };

        foreach (var package in deployment.Packages)
        {
            version.RequiredFiles.Add(package.Key);
            version.FileHashes[package.Key] = package.Value.Checksum;
        }

        await CacheVersionAsync(version, cancellationToken);
        return version;
    }

    public async Task<List<RobloxVersion>> GetVersionHistoryAsync(RobloxChannel channel, int maxVersions = 50, CancellationToken cancellationToken = default)
    {
        await _cacheLock.WaitAsync(cancellationToken);
        try
        {
            if (_versionCache.ContainsKey(channel) &&
                (DateTime.UtcNow - _lastCacheUpdate).TotalMinutes < CacheExpirationMinutes)
            {
                return _versionCache[channel];
            }

            var deployHistory = await _urlResolver.GetDeployHistoryAsync(cancellationToken);
            var versions = new List<RobloxVersion>();

            int count = 0;
            foreach (var versionHash in deployHistory.Take(maxVersions))
            {
                if (count >= maxVersions) break;

                try
                {
                    var deployment = await _urlResolver.GetDeploymentManifestAsync(versionHash, channel, cancellationToken);

                    var version = new RobloxVersion
                    {
                        VersionHash = versionHash,
                        ClientVersion = versionHash,
                        ReleaseDate = DateTime.UtcNow.AddDays(-count),
                        Channel = channel,
                        IsStudioVersion = channel == RobloxChannel.Studio || channel == RobloxChannel.StudioCanary,
                        TotalSize = deployment.Packages.Sum(p => p.Value.Size)
                    };

                    foreach (var package in deployment.Packages)
                    {
                        version.RequiredFiles.Add(package.Key);
                        version.FileHashes[package.Key] = package.Value.Checksum;
                    }

                    if (versions.Count > 0)
                    {
                        version.PreviousVersion = versions.Last().VersionHash;
                    }

                    versions.Add(version);
                    count++;
                }
                catch
                {
                    continue;
                }
            }

            _versionCache[channel] = versions;
            _lastCacheUpdate = DateTime.UtcNow;

            return versions;
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersionHash, RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        var latestVersion = await GetLatestVersionAsync(channel, cancellationToken);
        var currentVersion = await GetVersionByHashAsync(currentVersionHash, channel, cancellationToken);

        var result = new UpdateCheckResult
        {
            CurrentVersion = currentVersion,
            LatestVersion = latestVersion,
            UpdateAvailable = currentVersionHash != latestVersion.VersionHash
        };

        if (result.UpdateAvailable)
        {
            result.IntermediateVersions = await GetIntermediateVersionsAsync(currentVersionHash, latestVersion.VersionHash, channel, cancellationToken);
            result.TotalDownloadSize = latestVersion.TotalSize;
            result.RecommendedStrategy = DetermineUpdateStrategy(currentVersion, latestVersion, result.IntermediateVersions);
        }

        return result;
    }

    private async Task<RobloxVersion?> GetVersionByHashAsync(string versionHash, RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        var cachedVersion = await LoadCachedVersionAsync(versionHash, cancellationToken);
        if (cachedVersion != null)
        {
            return cachedVersion;
        }

        try
        {
            var deployment = await _urlResolver.GetDeploymentManifestAsync(versionHash, channel, cancellationToken);

            var version = new RobloxVersion
            {
                VersionHash = versionHash,
                ClientVersion = versionHash,
                ReleaseDate = DateTime.UtcNow,
                Channel = channel,
                IsStudioVersion = channel == RobloxChannel.Studio || channel == RobloxChannel.StudioCanary,
                TotalSize = deployment.Packages.Sum(p => p.Value.Size)
            };

            foreach (var package in deployment.Packages)
            {
                version.RequiredFiles.Add(package.Key);
                version.FileHashes[package.Key] = package.Value.Checksum;
            }

            await CacheVersionAsync(version, cancellationToken);
            return version;
        }
        catch
        {
            return null;
        }
    }

    private async Task<List<RobloxVersion>> GetIntermediateVersionsAsync(string fromVersion, string toVersion, RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        var versionHistory = await GetVersionHistoryAsync(channel, 100, cancellationToken);

        int fromIndex = versionHistory.FindIndex(v => v.VersionHash == fromVersion);
        int toIndex = versionHistory.FindIndex(v => v.VersionHash == toVersion);

        if (fromIndex == -1 || toIndex == -1 || fromIndex <= toIndex)
        {
            return new List<RobloxVersion>();
        }

        return versionHistory.GetRange(toIndex + 1, fromIndex - toIndex - 1);
    }

    private UpdateStrategy DetermineUpdateStrategy(RobloxVersion? current, RobloxVersion latest, List<RobloxVersion> intermediates)
    {
        if (current == null)
        {
            return UpdateStrategy.FullDownload;
        }

        if (intermediates.Count == 0)
        {
            return UpdateStrategy.FullDownload;
        }

        if (intermediates.Count <= 3)
        {
            long incrementalSize = intermediates.Sum(v => v.TotalSize);
            if (incrementalSize < latest.TotalSize * 0.7)
            {
                return UpdateStrategy.IncrementalPatch;
            }
        }

        var changedFiles = latest.FileHashes.Keys.Except(current.FileHashes.Keys).Count();
        var totalFiles = latest.FileHashes.Count;

        if (changedFiles < totalFiles * 0.3)
        {
            return UpdateStrategy.DeltaPatch;
        }

        if (intermediates.Count > 10)
        {
            return UpdateStrategy.FastForward;
        }

        return UpdateStrategy.FullDownload;
    }

    public async Task<VersionManifest> GenerateManifestAsync(CancellationToken cancellationToken = default)
    {
        var manifest = new VersionManifest
        {
            GeneratedAt = DateTime.UtcNow
        };

        foreach (RobloxChannel channel in Enum.GetValues(typeof(RobloxChannel)))
        {
            try
            {
                var latestVersion = await GetLatestVersionAsync(channel, cancellationToken);
                manifest.AvailableVersions[channel] = new List<RobloxVersion> { latestVersion };
                manifest.ChannelLatestVersions[channel.ToString()] = latestVersion.VersionHash;
            }
            catch
            {
                continue;
            }
        }

        return manifest;
    }

    private async Task CacheVersionAsync(RobloxVersion version, CancellationToken cancellationToken = default)
    {
        string cacheFilePath = Path.Combine(_cacheDirectory, $"{version.VersionHash}.json");

        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(version, options);
            await File.WriteAllTextAsync(cacheFilePath, json, cancellationToken);
        }
        catch
        {
        }
    }

    private async Task<RobloxVersion?> LoadCachedVersionAsync(string versionHash, CancellationToken cancellationToken = default)
    {
        string cacheFilePath = Path.Combine(_cacheDirectory, $"{versionHash}.json");

        if (!File.Exists(cacheFilePath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(cacheFilePath, cancellationToken);
            return JsonSerializer.Deserialize<RobloxVersion>(json);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ValidateVersionIntegrityAsync(string versionHash, string installDirectory, CancellationToken cancellationToken = default)
    {
        var version = await GetVersionByHashAsync(versionHash, RobloxChannel.Live, cancellationToken);
        if (version == null)
        {
            return false;
        }

        foreach (var fileHash in version.FileHashes)
        {
            string filePath = Path.Combine(installDirectory, fileHash.Key);
            if (!File.Exists(filePath))
            {
                return false;
            }

            string actualHash = await ComputeFileHashAsync(filePath, cancellationToken);
            if (actualHash != fileHash.Value)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken = default)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await md5.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public async Task CleanupOldCacheAsync(int maxAgeDays = 30, CancellationToken cancellationToken = default)
    {
        var cacheFiles = Directory.GetFiles(_cacheDirectory, "*.json");
        var cutoffDate = DateTime.UtcNow.AddDays(-maxAgeDays);

        foreach (var file in cacheFiles)
        {
            var fileInfo = new FileInfo(file);
            if (fileInfo.LastWriteTimeUtc < cutoffDate)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                }
            }
        }

        await Task.CompletedTask;
    }
}
