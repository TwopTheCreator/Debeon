using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using Debeon.ThirdParty.Models;

namespace Debeon.ThirdParty.Core;

public class DownloadManager
{
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, DownloadTask> _activeTasks;
    private readonly ConcurrentQueue<DownloadTask> _queuedTasks;
    private readonly SemaphoreSlim _concurrencyLimiter;
    private readonly int _maxConcurrentDownloads;
    private readonly int _bufferSize;
    private readonly CancellationTokenSource _managerCancellation;
    private Task? _downloadWorker;

    public event EventHandler<DownloadProgress>? ProgressChanged;
    public event EventHandler<DownloadTask>? TaskCompleted;
    public event EventHandler<DownloadTask>? TaskFailed;

    public DownloadManager(HttpClient httpClient, int maxConcurrentDownloads = 4, int bufferSizeKb = 8192)
    {
        _httpClient = httpClient;
        _maxConcurrentDownloads = maxConcurrentDownloads;
        _bufferSize = bufferSizeKb * 1024;
        _activeTasks = new ConcurrentDictionary<string, DownloadTask>();
        _queuedTasks = new ConcurrentQueue<DownloadTask>();
        _concurrencyLimiter = new SemaphoreSlim(maxConcurrentDownloads, maxConcurrentDownloads);
        _managerCancellation = new CancellationTokenSource();
    }

    public void Start()
    {
        if (_downloadWorker == null || _downloadWorker.IsCompleted)
        {
            _downloadWorker = Task.Run(ProcessDownloadQueueAsync);
        }
    }

    public void Stop()
    {
        _managerCancellation.Cancel();
    }

    public string QueueDownload(string url, string destinationPath, string? expectedHash = null, Dictionary<string, object>? metadata = null)
    {
        var task = new DownloadTask
        {
            Url = url,
            DestinationPath = destinationPath,
            Status = DownloadStatus.Queued,
            ExpectedHash = expectedHash,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        _queuedTasks.Enqueue(task);
        return task.TaskId;
    }

    public DownloadBatch QueueBatch(List<(string Url, string DestinationPath, string? ExpectedHash)> downloads)
    {
        var batch = new DownloadBatch
        {
            ConcurrentDownloads = _maxConcurrentDownloads
        };

        foreach (var (url, destinationPath, expectedHash) in downloads)
        {
            var task = new DownloadTask
            {
                Url = url,
                DestinationPath = destinationPath,
                Status = DownloadStatus.Queued,
                ExpectedHash = expectedHash
            };

            task.Metadata["BatchId"] = batch.BatchId;
            batch.Tasks.Add(task);
            _queuedTasks.Enqueue(task);
        }

        batch.TotalBytes = batch.Tasks.Sum(t => t.TotalBytes);
        return batch;
    }

    private async Task ProcessDownloadQueueAsync()
    {
        while (!_managerCancellation.Token.IsCancellationRequested)
        {
            if (_queuedTasks.TryDequeue(out var task))
            {
                await _concurrencyLimiter.WaitAsync(_managerCancellation.Token);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await ExecuteDownloadAsync(task, _managerCancellation.Token);
                    }
                    finally
                    {
                        _concurrencyLimiter.Release();
                    }
                }, _managerCancellation.Token);
            }
            else
            {
                await Task.Delay(100, _managerCancellation.Token);
            }
        }
    }

    private async Task ExecuteDownloadAsync(DownloadTask task, CancellationToken cancellationToken)
    {
        _activeTasks[task.TaskId] = task;
        task.Status = DownloadStatus.Downloading;
        task.StartTime = DateTime.UtcNow;

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var directory = Path.GetDirectoryName(task.DestinationPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var response = await _httpClient.GetAsync(task.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            response.EnsureSuccessStatusCode();

            task.TotalBytes = response.Content.Headers.ContentLength ?? 0;

            using var contentStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var fileStream = new FileStream(task.DestinationPath, FileMode.Create, FileAccess.Write, FileShare.None, _bufferSize, true);

            var buffer = new byte[_bufferSize];
            int bytesRead;
            long totalBytesRead = 0;
            var lastProgressUpdate = DateTime.UtcNow;

            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalBytesRead += bytesRead;
                task.DownloadedBytes = totalBytesRead;

                if ((DateTime.UtcNow - lastProgressUpdate).TotalMilliseconds >= 500)
                {
                    double elapsedSeconds = stopwatch.Elapsed.TotalSeconds;
                    double speed = elapsedSeconds > 0 ? totalBytesRead / elapsedSeconds : 0;
                    task.AverageSpeed = speed;

                    if (speed > 0 && task.TotalBytes > 0)
                    {
                        long remainingBytes = task.TotalBytes - totalBytesRead;
                        double remainingSeconds = remainingBytes / speed;
                        task.EstimatedTimeRemaining = TimeSpan.FromSeconds(remainingSeconds);
                    }

                    ProgressChanged?.Invoke(this, new DownloadProgress
                    {
                        TaskId = task.TaskId,
                        BytesDownloaded = totalBytesRead,
                        TotalBytes = task.TotalBytes,
                        Percentage = task.ProgressPercentage,
                        SpeedBytesPerSecond = speed,
                        TimeElapsed = stopwatch.Elapsed,
                        EstimatedTimeRemaining = task.EstimatedTimeRemaining
                    });

                    lastProgressUpdate = DateTime.UtcNow;
                }
            }

            stopwatch.Stop();

            task.Status = DownloadStatus.Verifying;

            if (!string.IsNullOrEmpty(task.ExpectedHash))
            {
                string actualHash = await ComputeFileHashAsync(task.DestinationPath, cancellationToken);
                task.ActualHash = actualHash;

                if (!actualHash.Equals(task.ExpectedHash, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException($"Hash mismatch. Expected: {task.ExpectedHash}, Actual: {actualHash}");
                }
            }

            task.Status = DownloadStatus.Completed;
            task.CompletionTime = DateTime.UtcNow;
            TaskCompleted?.Invoke(this, task);
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Failed;
            task.ErrorMessage = ex.Message;
            task.RetryCount++;

            if (task.RetryCount < task.MaxRetries)
            {
                task.Status = DownloadStatus.Queued;
                task.DownloadedBytes = 0;
                _queuedTasks.Enqueue(task);
            }
            else
            {
                TaskFailed?.Invoke(this, task);
            }
        }
        finally
        {
            _activeTasks.TryRemove(task.TaskId, out _);
        }
    }

    private async Task<string> ComputeFileHashAsync(string filePath, CancellationToken cancellationToken)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);

        var hashBytes = await md5.ComputeHashAsync(stream, cancellationToken);
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
    }

    public DownloadTask? GetTaskStatus(string taskId)
    {
        return _activeTasks.TryGetValue(taskId, out var task) ? task : null;
    }

    public List<DownloadTask> GetAllActiveTasks()
    {
        return _activeTasks.Values.ToList();
    }

    public async Task<bool> DownloadFileAsync(string url, string destinationPath, string? expectedHash = null, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var task = new DownloadTask
        {
            Url = url,
            DestinationPath = destinationPath,
            ExpectedHash = expectedHash,
            Status = DownloadStatus.Downloading,
            StartTime = DateTime.UtcNow
        };

        EventHandler<DownloadProgress>? progressHandler = null;
        if (progress != null)
        {
            progressHandler = (sender, downloadProgress) =>
            {
                if (downloadProgress.TaskId == task.TaskId)
                {
                    progress.Report(downloadProgress);
                }
            };
            ProgressChanged += progressHandler;
        }

        try
        {
            await ExecuteDownloadAsync(task, cancellationToken);
            return task.Status == DownloadStatus.Completed;
        }
        finally
        {
            if (progressHandler != null)
            {
                ProgressChanged -= progressHandler;
            }
        }
    }

    public async Task<DownloadBatch> DownloadBatchAsync(List<(string Url, string DestinationPath, string? ExpectedHash)> downloads, IProgress<DownloadProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var batch = QueueBatch(downloads);

        EventHandler<DownloadProgress>? progressHandler = null;
        if (progress != null)
        {
            progressHandler = (sender, downloadProgress) =>
            {
                var batchTask = batch.Tasks.FirstOrDefault(t => t.TaskId == downloadProgress.TaskId);
                if (batchTask != null)
                {
                    progress.Report(downloadProgress);
                }
            };
            ProgressChanged += progressHandler;
        }

        var completionTasks = batch.Tasks.Select(t => WaitForTaskCompletionAsync(t.TaskId, cancellationToken)).ToList();

        try
        {
            await Task.WhenAll(completionTasks);

            batch.Status = batch.Tasks.All(t => t.Status == DownloadStatus.Completed)
                ? BatchStatus.Completed
                : batch.Tasks.Any(t => t.Status == DownloadStatus.Completed)
                    ? BatchStatus.PartiallyCompleted
                    : BatchStatus.Failed;

            batch.DownloadedBytes = batch.Tasks.Sum(t => t.DownloadedBytes);
        }
        finally
        {
            if (progressHandler != null)
            {
                ProgressChanged -= progressHandler;
            }
        }

        return batch;
    }

    private async Task WaitForTaskCompletionAsync(string taskId, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (_activeTasks.TryGetValue(taskId, out var task))
            {
                if (task.Status == DownloadStatus.Completed || task.Status == DownloadStatus.Failed || task.Status == DownloadStatus.Cancelled)
                {
                    return;
                }
            }
            else
            {
                var queuedTask = _queuedTasks.FirstOrDefault(t => t.TaskId == taskId);
                if (queuedTask == null)
                {
                    return;
                }
            }

            await Task.Delay(100, cancellationToken);
        }
    }

    public void CancelTask(string taskId)
    {
        if (_activeTasks.TryGetValue(taskId, out var task))
        {
            task.Status = DownloadStatus.Cancelled;
        }
    }

    public void CancelAll()
    {
        foreach (var task in _activeTasks.Values)
        {
            task.Status = DownloadStatus.Cancelled;
        }

        while (_queuedTasks.TryDequeue(out var task))
        {
            task.Status = DownloadStatus.Cancelled;
        }
    }

    public void Dispose()
    {
        Stop();
        _managerCancellation.Dispose();
        _concurrencyLimiter.Dispose();
    }
}
