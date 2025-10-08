namespace Debeon.ThirdParty.Models;

public class DownloadTask
{
    public string TaskId { get; set; } = Guid.NewGuid().ToString();
    public string Url { get; set; } = string.Empty;
    public string DestinationPath { get; set; } = string.Empty;
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public DownloadStatus Status { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? CompletionTime { get; set; }
    public int RetryCount { get; set; }
    public int MaxRetries { get; set; } = 5;
    public string? ErrorMessage { get; set; }
    public double ProgressPercentage => TotalBytes > 0 ? (DownloadedBytes * 100.0 / TotalBytes) : 0;
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public double AverageSpeed { get; set; }
    public string? ExpectedHash { get; set; }
    public string? ActualHash { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public enum DownloadStatus
{
    Queued,
    Downloading,
    Paused,
    Verifying,
    Completed,
    Failed,
    Cancelled
}

public class DownloadBatch
{
    public string BatchId { get; set; } = Guid.NewGuid().ToString();
    public List<DownloadTask> Tasks { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int ConcurrentDownloads { get; set; } = 4;
    public long TotalBytes { get; set; }
    public long DownloadedBytes { get; set; }
    public double OverallProgress => TotalBytes > 0 ? (DownloadedBytes * 100.0 / TotalBytes) : 0;
    public BatchStatus Status { get; set; }
}

public enum BatchStatus
{
    Pending,
    InProgress,
    Completed,
    Failed,
    PartiallyCompleted
}

public class DownloadProgress
{
    public string TaskId { get; set; } = string.Empty;
    public long BytesDownloaded { get; set; }
    public long TotalBytes { get; set; }
    public double Percentage { get; set; }
    public double SpeedBytesPerSecond { get; set; }
    public TimeSpan TimeElapsed { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public string FormattedSpeed => FormatSpeed(SpeedBytesPerSecond);
    public string FormattedBytesDownloaded => FormatBytes(BytesDownloaded);
    public string FormattedTotalBytes => FormatBytes(TotalBytes);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatSpeed(double bytesPerSecond)
    {
        return $"{FormatBytes((long)bytesPerSecond)}/s";
    }
}
