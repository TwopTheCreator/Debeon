using Debeon.ThirdParty.Core;
using Debeon.ThirdParty.Models;
using Debeon.ThirdParty.Services;

namespace Debeon.ThirdParty;

public class ThirdPartyManager : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly RobloxUrlResolver _urlResolver;
    private readonly VersionManager _versionManager;
    private readonly DownloadManager _downloadManager;
    private readonly AutoUpdater _autoUpdater;
    private readonly PackageExtractor _packageExtractor;
    private readonly IntegrityVerifier _integrityVerifier;
    private readonly CacheManager _cacheManager;

    private readonly string _baseDirectory;
    private readonly string _installDirectory;
    private readonly string _tempDirectory;
    private readonly string _cacheDirectory;

    public RobloxUrlResolver UrlResolver => _urlResolver;
    public VersionManager VersionManager => _versionManager;
    public DownloadManager DownloadManager => _downloadManager;
    public AutoUpdater AutoUpdater => _autoUpdater;
    public PackageExtractor PackageExtractor => _packageExtractor;
    public IntegrityVerifier IntegrityVerifier => _integrityVerifier;
    public CacheManager CacheManager => _cacheManager;

    public ThirdPartyManager(string baseDirectory, HttpClient? httpClient = null)
    {
        _baseDirectory = baseDirectory;
        _installDirectory = Path.Combine(_baseDirectory, "Installations");
        _tempDirectory = Path.Combine(_baseDirectory, "Temp");
        _cacheDirectory = Path.Combine(_baseDirectory, "Cache");

        EnsureDirectoriesExist();

        _httpClient = httpClient ?? new HttpClient
        {
            Timeout = TimeSpan.FromMinutes(30)
        };

        _urlResolver = new RobloxUrlResolver(_httpClient);
        _versionManager = new VersionManager(_urlResolver, _cacheDirectory);
        _downloadManager = new DownloadManager(_httpClient, maxConcurrentDownloads: 4);
        _autoUpdater = new AutoUpdater(_versionManager, _downloadManager, _urlResolver, _installDirectory, _tempDirectory);
        _packageExtractor = new PackageExtractor(_tempDirectory);
        _integrityVerifier = new IntegrityVerifier();
        _cacheManager = new CacheManager(_cacheDirectory, maxCacheSizeMb: 2048);
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(_baseDirectory);
        Directory.CreateDirectory(_installDirectory);
        Directory.CreateDirectory(_tempDirectory);
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<RobloxVersion> GetLatestVersionAsync(RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"latest_version_{channel}";
        var cachedVersion = await _cacheManager.GetAsync<RobloxVersion>(cacheKey, cancellationToken);

        if (cachedVersion != null)
        {
            return cachedVersion;
        }

        var version = await _versionManager.GetLatestVersionAsync(channel, cancellationToken);
        await _cacheManager.SetAsync(cacheKey, version, TimeSpan.FromMinutes(30), cancellationToken);

        return version;
    }

    public async Task<bool> DownloadAndInstallVersionAsync(RobloxVersion version, IProgress<UpdateProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var deployment = await _urlResolver.GetDeploymentManifestAsync(version.VersionHash, version.Channel, cancellationToken);

        var updateProgress = new UpdateProgress
        {
            TotalSteps = 4,
            CurrentStep = 1,
            StepDescription = "Downloading packages",
            OverallProgress = 0
        };
        progress?.Report(updateProgress);

        var downloadTasks = new List<(string Url, string DestinationPath, string? ExpectedHash)>();
        foreach (var package in deployment.Packages)
        {
            string destPath = Path.Combine(_tempDirectory, package.Key);
            downloadTasks.Add((package.Value.DownloadUrl, destPath, package.Value.Checksum));
        }

        var downloadProgress = new Progress<DownloadProgress>(p =>
        {
            updateProgress.DownloadProgress = p.Percentage;
            updateProgress.OverallProgress = p.Percentage * 0.5;
            progress?.Report(updateProgress);
        });

        var batch = await _downloadManager.DownloadBatchAsync(downloadTasks, downloadProgress, cancellationToken);

        if (batch.Status != BatchStatus.Completed)
        {
            return false;
        }

        updateProgress.CurrentStep = 2;
        updateProgress.StepDescription = "Verifying files";
        updateProgress.OverallProgress = 50;
        progress?.Report(updateProgress);

        bool isValid = await _integrityVerifier.VerifyDeploymentIntegrityAsync(deployment, _tempDirectory, cancellationToken);
        if (!isValid)
        {
            return false;
        }

        updateProgress.CurrentStep = 3;
        updateProgress.StepDescription = "Extracting packages";
        updateProgress.OverallProgress = 70;
        progress?.Report(updateProgress);

        string versionInstallDir = Path.Combine(_installDirectory, version.VersionHash);
        var extractedPaths = await _packageExtractor.ExtractMultiplePackagesAsync(
            downloadTasks.Select(t => t.DestinationPath).ToList(),
            versionInstallDir,
            cancellationToken
        );

        updateProgress.CurrentStep = 4;
        updateProgress.StepDescription = "Finalizing installation";
        updateProgress.OverallProgress = 90;
        progress?.Report(updateProgress);

        await CleanupTemporaryFilesAsync(cancellationToken);

        updateProgress.OverallProgress = 100;
        updateProgress.StepDescription = "Installation completed";
        progress?.Report(updateProgress);

        return true;
    }

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersionHash, RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        return await _versionManager.CheckForUpdatesAsync(currentVersionHash, channel, cancellationToken);
    }

    public async Task<bool> PerformAutoUpdateAsync(RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        var latestVersion = await GetLatestVersionAsync(channel, cancellationToken);

        var updateCheck = new UpdateCheckResult
        {
            UpdateAvailable = true,
            LatestVersion = latestVersion,
            TotalDownloadSize = latestVersion.TotalSize,
            RecommendedStrategy = UpdateStrategy.FullDownload
        };

        return await _autoUpdater.PerformUpdateAsync(updateCheck, cancellationToken);
    }

    public async Task<List<RobloxVersion>> GetVersionHistoryAsync(RobloxChannel channel, int maxVersions = 50, CancellationToken cancellationToken = default)
    {
        return await _versionManager.GetVersionHistoryAsync(channel, maxVersions, cancellationToken);
    }

    public async Task<VersionManifest> GenerateVersionManifestAsync(CancellationToken cancellationToken = default)
    {
        return await _versionManager.GenerateManifestAsync(cancellationToken);
    }

    public async Task<bool> ValidateInstallationAsync(string versionHash, CancellationToken cancellationToken = default)
    {
        string installPath = Path.Combine(_installDirectory, versionHash);
        return await _versionManager.ValidateVersionIntegrityAsync(versionHash, installPath, cancellationToken);
    }

    public async Task CleanupTemporaryFilesAsync(CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (Directory.Exists(_tempDirectory))
            {
                var files = Directory.GetFiles(_tempDirectory);
                foreach (var file in files)
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
        }, cancellationToken);
    }

    public async Task CleanupOldVersionsAsync(int keepLatestVersions = 3, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (!Directory.Exists(_installDirectory))
            {
                return;
            }

            var versionDirs = Directory.GetDirectories(_installDirectory)
                .Select(d => new DirectoryInfo(d))
                .OrderByDescending(d => d.CreationTimeUtc)
                .Skip(keepLatestVersions);

            foreach (var dir in versionDirs)
            {
                try
                {
                    dir.Delete(true);
                }
                catch
                {
                }
            }
        }, cancellationToken);
    }

    public async Task<CacheStatistics> GetCacheStatisticsAsync(CancellationToken cancellationToken = default)
    {
        return await _cacheManager.GetStatisticsAsync(cancellationToken);
    }

    public async Task ClearAllCachesAsync(CancellationToken cancellationToken = default)
    {
        await _cacheManager.ClearAsync(cancellationToken);
        await _versionManager.CleanupOldCacheAsync(0, cancellationToken);
    }

    public void Dispose()
    {
        _downloadManager.Dispose();
        _autoUpdater.Dispose();
        _httpClient.Dispose();
    }
}
