namespace Debeon.ThirdParty.Models;

public class RobloxVersion
{
    public string VersionHash { get; set; } = string.Empty;
    public string ClientVersion { get; set; } = string.Empty;
    public DateTime ReleaseDate { get; set; }
    public RobloxChannel Channel { get; set; }
    public Dictionary<string, string> FileHashes { get; set; } = new();
    public long TotalSize { get; set; }
    public bool IsStudioVersion { get; set; }
    public string? PreviousVersion { get; set; }
    public List<string> RequiredFiles { get; set; } = new();
    public Dictionary<string, string> Metadata { get; set; } = new();
}

public enum RobloxChannel
{
    Live,
    ZCanary,
    ZIntegration,
    ZNext,
    Studio,
    StudioCanary
}

public class RobloxDeployment
{
    public string DeploymentId { get; set; } = string.Empty;
    public string VersionGuid { get; set; } = string.Empty;
    public Dictionary<string, PackageManifest> Packages { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string Platform { get; set; } = "windows";
    public string Architecture { get; set; } = "x64";
}

public class PackageManifest
{
    public string PackageName { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public long Size { get; set; }
    public List<FileEntry> Files { get; set; } = new();
    public string DownloadUrl { get; set; } = string.Empty;
}

public class FileEntry
{
    public string RelativePath { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public long Size { get; set; }
    public bool IsCompressed { get; set; }
    public string? CompressedHash { get; set; }
    public long? CompressedSize { get; set; }
}

public class VersionManifest
{
    public string ManifestVersion { get; set; } = "1.0";
    public DateTime GeneratedAt { get; set; }
    public Dictionary<RobloxChannel, List<RobloxVersion>> AvailableVersions { get; set; } = new();
    public Dictionary<string, string> ChannelLatestVersions { get; set; } = new();
}

public class UpdateCheckResult
{
    public bool UpdateAvailable { get; set; }
    public RobloxVersion? CurrentVersion { get; set; }
    public RobloxVersion? LatestVersion { get; set; }
    public List<RobloxVersion> IntermediateVersions { get; set; } = new();
    public long TotalDownloadSize { get; set; }
    public UpdateStrategy RecommendedStrategy { get; set; }
}

public enum UpdateStrategy
{
    FullDownload,
    IncrementalPatch,
    DeltaPatch,
    FastForward
}
