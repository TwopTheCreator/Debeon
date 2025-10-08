using System.Text.Json;
using System.Text.RegularExpressions;
using Debeon.ThirdParty.Models;

namespace Debeon.ThirdParty.Core;

public class RobloxUrlResolver
{
    private const string SetupApiBaseUrl = "https://setup.rbxcdn.com";
    private const string ClientSettingsApiUrl = "https://clientsettingscdn.roblox.com";
    private const string DeployHistoryUrl = "https://setup.rbxcdn.com/DeployHistory.txt";

    private readonly HttpClient _httpClient;
    private readonly Dictionary<RobloxChannel, string> _channelMappings;
    private readonly SemaphoreSlim _rateLimiter;

    public RobloxUrlResolver(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _rateLimiter = new SemaphoreSlim(10, 10);

        _channelMappings = new Dictionary<RobloxChannel, string>
        {
            { RobloxChannel.Live, "version-" },
            { RobloxChannel.ZCanary, "versionQTStudio-" },
            { RobloxChannel.ZIntegration, "version-" },
            { RobloxChannel.ZNext, "version-" },
            { RobloxChannel.Studio, "versionQTStudio-" },
            { RobloxChannel.StudioCanary, "versionQTStudio-" }
        };
    }

    public async Task<string> GetLatestVersionHashAsync(RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            string channelPrefix = _channelMappings[channel];
            string url = $"{SetupApiBaseUrl}/{channelPrefix}";

            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            string versionHash = await response.Content.ReadAsStringAsync(cancellationToken);
            return versionHash.Trim();
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<RobloxDeployment> GetDeploymentManifestAsync(string versionHash, RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            string channelPrefix = _channelMappings[channel];
            string manifestUrl = $"{SetupApiBaseUrl}/{channelPrefix}{versionHash}-rbxPkgManifest.txt";

            var response = await _httpClient.GetAsync(manifestUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            string manifestContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseDeploymentManifest(manifestContent, versionHash);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private RobloxDeployment ParseDeploymentManifest(string content, string versionHash)
    {
        var deployment = new RobloxDeployment
        {
            DeploymentId = versionHash,
            VersionGuid = versionHash,
            Timestamp = DateTime.UtcNow
        };

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var parts = line.Trim().Split(' ');
            if (parts.Length < 2) continue;

            string packageName = parts[0];
            string checksum = parts[1];
            long size = parts.Length > 2 && long.TryParse(parts[2], out long s) ? s : 0;

            deployment.Packages[packageName] = new PackageManifest
            {
                PackageName = packageName,
                Checksum = checksum,
                Size = size,
                DownloadUrl = $"{SetupApiBaseUrl}/{versionHash}-{packageName}"
            };
        }

        return deployment;
    }

    public async Task<Dictionary<string, FileEntry>> GetPackageFileListAsync(string versionHash, string packageName, CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            string fileListUrl = $"{SetupApiBaseUrl}/{versionHash}-{packageName}.txt";

            var response = await _httpClient.GetAsync(fileListUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new Dictionary<string, FileEntry>();
            }

            string fileListContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseFileList(fileListContent);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private Dictionary<string, FileEntry> ParseFileList(string content)
    {
        var fileEntries = new Dictionary<string, FileEntry>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Trim().Split('\t');
            if (parts.Length < 3) continue;

            string filePath = parts[0];
            string hash = parts[1];
            long size = long.TryParse(parts[2], out long s) ? s : 0;

            fileEntries[filePath] = new FileEntry
            {
                RelativePath = filePath,
                Hash = hash,
                Size = size,
                IsCompressed = filePath.EndsWith(".zip") || filePath.EndsWith(".gz")
            };
        }

        return fileEntries;
    }

    public async Task<List<string>> GetDeployHistoryAsync(CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            var response = await _httpClient.GetAsync(DeployHistoryUrl, cancellationToken);
            response.EnsureSuccessStatusCode();

            string historyContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return ParseDeployHistory(historyContent);
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    private List<string> ParseDeployHistory(string content)
    {
        var history = new List<string>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (!string.IsNullOrEmpty(trimmed))
            {
                history.Add(trimmed);
            }
        }

        return history;
    }

    public string BuildPackageDownloadUrl(string versionHash, string packageName)
    {
        return $"{SetupApiBaseUrl}/{versionHash}-{packageName}";
    }

    public async Task<Dictionary<string, object>> GetClientSettingsAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            string settingsUrl = $"{ClientSettingsApiUrl}/v2/client-version/WindowsPlayer/channel/live";

            var request = new HttpRequestMessage(HttpMethod.Get, settingsUrl);
            request.Headers.Add("ApiKey", apiKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            string jsonContent = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent) ?? new Dictionary<string, object>();
        }
        finally
        {
            _rateLimiter.Release();
        }
    }

    public async Task<bool> ValidateVersionExistsAsync(string versionHash, RobloxChannel channel, CancellationToken cancellationToken = default)
    {
        try
        {
            string channelPrefix = _channelMappings[channel];
            string testUrl = $"{SetupApiBaseUrl}/{channelPrefix}{versionHash}-rbxPkgManifest.txt";

            var request = new HttpRequestMessage(HttpMethod.Head, testUrl);
            var response = await _httpClient.SendAsync(request, cancellationToken);

            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public string ExtractVersionFromUrl(string url)
    {
        var match = Regex.Match(url, @"version-([a-f0-9]+)");
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return string.Empty;
    }

    public RobloxChannel DetermineChannelFromUrl(string url)
    {
        if (url.Contains("versionQTStudio"))
        {
            if (url.Contains("canary"))
                return RobloxChannel.StudioCanary;
            return RobloxChannel.Studio;
        }

        if (url.Contains("zcanary") || url.Contains("ZCanary"))
            return RobloxChannel.ZCanary;
        if (url.Contains("zintegration") || url.Contains("ZIntegration"))
            return RobloxChannel.ZIntegration;
        if (url.Contains("znext") || url.Contains("ZNext"))
            return RobloxChannel.ZNext;

        return RobloxChannel.Live;
    }
}
