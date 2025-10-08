using System.Diagnostics;
using System.Text.Json;
using Debeon.ThirdParty.Models;

namespace Debeon.ThirdParty.Core;

public class AutoUpdater
{
    private readonly VersionManager _versionManager;
    private readonly DownloadManager _downloadManager;
    private readonly RobloxUrlResolver _urlResolver;
    private readonly string _installDirectory;
    private readonly string _tempDirectory;
    private readonly Timer _updateCheckTimer;
    private readonly SemaphoreSlim _updateLock;
    private TimeSpan _updateCheckInterval;
    private bool _autoUpdateEnabled;

    public event EventHandler<UpdateCheckResult>? UpdateAvailable;
    public event EventHandler<UpdateProgress>? UpdateProgressChanged;
    public event EventHandler<UpdateCompletedEventArgs>? UpdateCompleted;
    public event EventHandler<UpdateFailedEventArgs>? UpdateFailed;

    public bool AutoUpdateEnabled
    {
        get => _autoUpdateEnabled;
        set => _autoUpdateEnabled = value;
    }

    public TimeSpan UpdateCheckInterval
    {
        get => _updateCheckInterval;
        set
        {
            _updateCheckInterval = value;
            _updateCheckTimer.Change(value, value);
        }
    }

    public AutoUpdater(
        VersionManager versionManager,
        DownloadManager downloadManager,
        RobloxUrlResolver urlResolver,
        string installDirectory,
        string tempDirectory)
    {
        _versionManager = versionManager;
        _downloadManager = downloadManager;
        _urlResolver = urlResolver;
        _installDirectory = installDirectory;
        _tempDirectory = tempDirectory;
        _updateLock = new SemaphoreSlim(1, 1);
        _updateCheckInterval = TimeSpan.FromHours(1);
        _autoUpdateEnabled = false;

        EnsureDirectoriesExist();

        _updateCheckTimer = new Timer(
            async _ => await CheckForUpdatesTimerCallbackAsync(),
            null,
            Timeout.Infinite,
            Timeout.Infinite
        );
    }

    private void EnsureDirectoriesExist()
    {
        if (!Directory.Exists(_installDirectory))
        {
            Directory.CreateDirectory(_installDirectory);
        }

        if (!Directory.Exists(_tempDirectory))
        {
            Directory.CreateDirectory(_tempDirectory);
        }
    }

    public void StartAutoUpdateChecks()
    {
        _autoUpdateEnabled = true;
        _updateCheckTimer.Change(_updateCheckInterval, _updateCheckInterval);
    }

    public void StopAutoUpdateChecks()
    {
        _autoUpdateEnabled = false;
        _updateCheckTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    private async Task CheckForUpdatesTimerCallbackAsync()
    {
        if (!_autoUpdateEnabled)
        {
            return;
        }

        try
        {
            var currentVersion = await GetCurrentInstalledVersionAsync();
            if (currentVersion != null)
            {
                var updateCheck = await _versionManager.CheckForUpdatesAsync(
                    currentVersion.VersionHash,
                    currentVersion.Channel,
                    CancellationToken.None
                );

                if (updateCheck.UpdateAvailable)
                {
                    UpdateAvailable?.Invoke(this, updateCheck);

                    if (_autoUpdateEnabled)
                    {
                        await PerformUpdateAsync(updateCheck, CancellationToken.None);
                    }
                }
            }
        }
        catch
        {
        }
    }

    public async Task<UpdateCheckResult> ManualUpdateCheckAsync(RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        var currentVersion = await GetCurrentInstalledVersionAsync(channel);

        if (currentVersion == null)
        {
            var latestVersion = await _versionManager.GetLatestVersionAsync(channel, cancellationToken);
            return new UpdateCheckResult
            {
                UpdateAvailable = true,
                LatestVersion = latestVersion,
                TotalDownloadSize = latestVersion.TotalSize,
                RecommendedStrategy = UpdateStrategy.FullDownload
            };
        }

        return await _versionManager.CheckForUpdatesAsync(
            currentVersion.VersionHash,
            channel,
            cancellationToken
        );
    }

    public async Task<bool> PerformUpdateAsync(UpdateCheckResult updateCheck, CancellationToken cancellationToken = default)
    {
        if (!updateCheck.UpdateAvailable || updateCheck.LatestVersion == null)
        {
            return false;
        }

        await _updateLock.WaitAsync(cancellationToken);

        try
        {
            var updateProgress = new UpdateProgress
            {
                TotalSteps = 5,
                CurrentStep = 1,
                StepDescription = "Preparing update",
                OverallProgress = 0
            };
            UpdateProgressChanged?.Invoke(this, updateProgress);

            await PrepareUpdateEnvironmentAsync(cancellationToken);

            updateProgress.CurrentStep = 2;
            updateProgress.StepDescription = "Downloading files";
            updateProgress.OverallProgress = 20;
            UpdateProgressChanged?.Invoke(this, updateProgress);

            var deployment = await _urlResolver.GetDeploymentManifestAsync(
                updateCheck.LatestVersion.VersionHash,
                updateCheck.LatestVersion.Channel,
                cancellationToken
            );

            var downloadTasks = new List<(string Url, string DestinationPath, string? ExpectedHash)>();
            foreach (var package in deployment.Packages)
            {
                string url = package.Value.DownloadUrl;
                string destPath = Path.Combine(_tempDirectory, package.Key);
                downloadTasks.Add((url, destPath, package.Value.Checksum));
            }

            var progress = new Progress<DownloadProgress>(p =>
            {
                updateProgress.DownloadProgress = p.Percentage;
                updateProgress.OverallProgress = 20 + (p.Percentage * 0.5);
                UpdateProgressChanged?.Invoke(this, updateProgress);
            });

            var batch = await _downloadManager.DownloadBatchAsync(downloadTasks, progress, cancellationToken);

            if (batch.Status != BatchStatus.Completed)
            {
                throw new Exception("Failed to download all required files");
            }

            updateProgress.CurrentStep = 3;
            updateProgress.StepDescription = "Verifying files";
            updateProgress.OverallProgress = 70;
            UpdateProgressChanged?.Invoke(this, updateProgress);

            await VerifyDownloadedFilesAsync(deployment, cancellationToken);

            updateProgress.CurrentStep = 4;
            updateProgress.StepDescription = "Installing update";
            updateProgress.OverallProgress = 80;
            UpdateProgressChanged?.Invoke(this, updateProgress);

            await InstallUpdateAsync(deployment, updateCheck.LatestVersion, cancellationToken);

            updateProgress.CurrentStep = 5;
            updateProgress.StepDescription = "Finalizing";
            updateProgress.OverallProgress = 95;
            UpdateProgressChanged?.Invoke(this, updateProgress);

            await FinalizeUpdateAsync(updateCheck.LatestVersion, cancellationToken);

            updateProgress.OverallProgress = 100;
            updateProgress.StepDescription = "Update completed";
            UpdateProgressChanged?.Invoke(this, updateProgress);

            UpdateCompleted?.Invoke(this, new UpdateCompletedEventArgs
            {
                OldVersion = updateCheck.CurrentVersion,
                NewVersion = updateCheck.LatestVersion,
                Success = true
            });

            return true;
        }
        catch (Exception ex)
        {
            UpdateFailed?.Invoke(this, new UpdateFailedEventArgs
            {
                Error = ex,
                FailedVersion = updateCheck.LatestVersion
            });

            return false;
        }
        finally
        {
            _updateLock.Release();
        }
    }

    private async Task PrepareUpdateEnvironmentAsync(CancellationToken cancellationToken)
    {
        if (Directory.Exists(_tempDirectory))
        {
            var tempFiles = Directory.GetFiles(_tempDirectory);
            foreach (var file in tempFiles)
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

    private async Task VerifyDownloadedFilesAsync(RobloxDeployment deployment, CancellationToken cancellationToken)
    {
        foreach (var package in deployment.Packages)
        {
            string filePath = Path.Combine(_tempDirectory, package.Key);

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Downloaded file not found: {package.Key}");
            }

            string actualHash = await ComputeFileHashAsync(filePath, cancellationToken);
            if (!actualHash.Equals(package.Value.Checksum, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"File verification failed for {package.Key}");
            }
        }
    }

    private async Task InstallUpdateAsync(RobloxDeployment deployment, RobloxVersion version, CancellationToken cancellationToken)
    {
        string versionInstallDir = Path.Combine(_installDirectory, version.VersionHash);

        if (!Directory.Exists(versionInstallDir))
        {
            Directory.CreateDirectory(versionInstallDir);
        }

        foreach (var package in deployment.Packages)
        {
            string sourcePath = Path.Combine(_tempDirectory, package.Key);
            string destPath = Path.Combine(versionInstallDir, package.Key);

            if (package.Key.EndsWith(".zip"))
            {
                string extractDir = Path.Combine(versionInstallDir, Path.GetFileNameWithoutExtension(package.Key));
                Directory.CreateDirectory(extractDir);
                System.IO.Compression.ZipFile.ExtractToDirectory(sourcePath, extractDir, true);
            }
            else
            {
                File.Copy(sourcePath, destPath, true);
            }
        }

        await Task.CompletedTask;
    }

    private async Task FinalizeUpdateAsync(RobloxVersion version, CancellationToken cancellationToken)
    {
        var versionInfo = new InstalledVersionInfo
        {
            Version = version,
            InstallDate = DateTime.UtcNow,
            InstallPath = Path.Combine(_installDirectory, version.VersionHash)
        };

        string versionInfoPath = Path.Combine(_installDirectory, "current_version.json");
        var options = new JsonSerializerOptions { WriteIndented = true };
        string json = JsonSerializer.Serialize(versionInfo, options);
        await File.WriteAllTextAsync(versionInfoPath, json, cancellationToken);

        var tempFiles = Directory.GetFiles(_tempDirectory);
        foreach (var file in tempFiles)
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

    private async Task<RobloxVersion?> GetCurrentInstalledVersionAsync(RobloxChannel? channel = null)
    {
        string versionInfoPath = Path.Combine(_installDirectory, "current_version.json");

        if (!File.Exists(versionInfoPath))
        {
            return null;
        }

        try
        {
            string json = await File.ReadAllTextAsync(versionInfoPath);
            var versionInfo = JsonSerializer.Deserialize<InstalledVersionInfo>(json);
            return versionInfo?.Version;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await md5.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public async Task RollbackToVersionAsync(string versionHash, CancellationToken cancellationToken = default)
    {
        string versionPath = Path.Combine(_installDirectory, versionHash);

        if (!Directory.Exists(versionPath))
        {
            throw new DirectoryNotFoundException($"Version {versionHash} not found in installation directory");
        }

        var version = await _versionManager.GetVersionHistoryAsync(RobloxChannel.Live, 100, cancellationToken);
        var targetVersion = version.FirstOrDefault(v => v.VersionHash == versionHash);

        if (targetVersion == null)
        {
            throw new InvalidOperationException($"Version {versionHash} not found in version history");
        }

        await FinalizeUpdateAsync(targetVersion, cancellationToken);
    }

    public async Task<List<RobloxVersion>> GetInstalledVersionsAsync(CancellationToken cancellationToken = default)
    {
        var installedVersions = new List<RobloxVersion>();
        var versionDirs = Directory.GetDirectories(_installDirectory);

        foreach (var dir in versionDirs)
        {
            string versionHash = Path.GetFileName(dir);
            var versionHistory = await _versionManager.GetVersionHistoryAsync(RobloxChannel.Live, 100, cancellationToken);
            var version = versionHistory.FirstOrDefault(v => v.VersionHash == versionHash);

            if (version != null)
            {
                installedVersions.Add(version);
            }
        }

        return installedVersions;
    }

    public void Dispose()
    {
        _updateCheckTimer.Dispose();
        _updateLock.Dispose();
    }
}

public class UpdateProgress
{
    public int TotalSteps { get; set; }
    public int CurrentStep { get; set; }
    public string StepDescription { get; set; } = string.Empty;
    public double OverallProgress { get; set; }
    public double DownloadProgress { get; set; }
}

public class UpdateCompletedEventArgs : EventArgs
{
    public RobloxVersion? OldVersion { get; set; }
    public RobloxVersion? NewVersion { get; set; }
    public bool Success { get; set; }
}

public class UpdateFailedEventArgs : EventArgs
{
    public Exception? Error { get; set; }
    public RobloxVersion? FailedVersion { get; set; }
}

public class InstalledVersionInfo
{
    public RobloxVersion Version { get; set; } = new();
    public DateTime InstallDate { get; set; }
    public string InstallPath { get; set; } = string.Empty;
}
