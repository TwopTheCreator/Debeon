using System.Collections.Concurrent;
using System.Text.Json;

namespace Debeon.ThirdParty.Services;

public class CacheManager
{
    private readonly string _cacheDirectory;
    private readonly ConcurrentDictionary<string, CacheEntry> _memoryCache;
    private readonly SemaphoreSlim _diskLock;
    private readonly long _maxCacheSizeBytes;
    private readonly TimeSpan _defaultExpiration;

    public CacheManager(string cacheDirectory, long maxCacheSizeMb = 1024, TimeSpan? defaultExpiration = null)
    {
        _cacheDirectory = cacheDirectory;
        _memoryCache = new ConcurrentDictionary<string, CacheEntry>();
        _diskLock = new SemaphoreSlim(1, 1);
        _maxCacheSizeBytes = maxCacheSizeMb * 1024 * 1024;
        _defaultExpiration = defaultExpiration ?? TimeSpan.FromHours(24);

        EnsureCacheDirectoryExists();
        LoadCacheIndex();
    }

    private void EnsureCacheDirectoryExists()
    {
        if (!Directory.Exists(_cacheDirectory))
        {
            Directory.CreateDirectory(_cacheDirectory);
        }
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_memoryCache.TryGetValue(key, out var entry))
        {
            if (!entry.IsExpired())
            {
                entry.LastAccessTime = DateTime.UtcNow;
                entry.AccessCount++;
                return entry.Value as T;
            }
            else
            {
                _memoryCache.TryRemove(key, out _);
            }
        }

        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            string filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                string json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var diskEntry = JsonSerializer.Deserialize<CacheEntry>(json);

                if (diskEntry != null && !diskEntry.IsExpired())
                {
                    diskEntry.LastAccessTime = DateTime.UtcNow;
                    diskEntry.AccessCount++;
                    _memoryCache[key] = diskEntry;
                    return diskEntry.Value as T;
                }
                else
                {
                    File.Delete(filePath);
                }
            }
        }
        finally
        {
            _diskLock.Release();
        }

        return null;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        var entry = new CacheEntry
        {
            Key = key,
            Value = value,
            CreatedTime = DateTime.UtcNow,
            LastAccessTime = DateTime.UtcNow,
            ExpirationTime = DateTime.UtcNow.Add(expiration ?? _defaultExpiration),
            AccessCount = 0
        };

        _memoryCache[key] = entry;

        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            await EnsureCacheSizeLimitAsync(cancellationToken);

            string filePath = GetCacheFilePath(key);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(entry, options);
            await File.WriteAllTextAsync(filePath, json, cancellationToken);
        }
        finally
        {
            _diskLock.Release();
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out var entry))
        {
            return !entry.IsExpired();
        }

        string filePath = GetCacheFilePath(key);
        if (File.Exists(filePath))
        {
            try
            {
                string json = await File.ReadAllTextAsync(filePath, cancellationToken);
                var diskEntry = JsonSerializer.Deserialize<CacheEntry>(json);
                return diskEntry != null && !diskEntry.IsExpired();
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.TryRemove(key, out _);

        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            string filePath = GetCacheFilePath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        finally
        {
            _diskLock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        _memoryCache.Clear();

        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.cache");
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
        finally
        {
            _diskLock.Release();
        }
    }

    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            var stats = new CacheStatistics
            {
                MemoryCacheEntries = _memoryCache.Count,
                TotalAccessCount = _memoryCache.Values.Sum(e => e.AccessCount)
            };

            var files = Directory.GetFiles(_cacheDirectory, "*.cache");
            stats.DiskCacheEntries = files.Length;

            long totalSize = 0;
            foreach (var file in files)
            {
                var fileInfo = new FileInfo(file);
                totalSize += fileInfo.Length;
            }
            stats.DiskCacheSizeBytes = totalSize;

            return stats;
        }
        finally
        {
            _diskLock.Release();
        }
    }

    private async Task EnsureCacheSizeLimitAsync(CancellationToken cancellationToken)
    {
        var files = Directory.GetFiles(_cacheDirectory, "*.cache");
        long totalSize = files.Sum(f => new FileInfo(f).Length);

        if (totalSize > _maxCacheSizeBytes)
        {
            var fileInfos = files.Select(f => new FileInfo(f))
                .OrderBy(f => f.LastAccessTime)
                .ToList();

            foreach (var fileInfo in fileInfos)
            {
                if (totalSize <= _maxCacheSizeBytes * 0.8)
                {
                    break;
                }

                try
                {
                    totalSize -= fileInfo.Length;
                    fileInfo.Delete();
                }
                catch
                {
                }
            }
        }

        await Task.CompletedTask;
    }

    private void LoadCacheIndex()
    {
        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.cache");
            foreach (var file in files.Take(100))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var entry = JsonSerializer.Deserialize<CacheEntry>(json);
                    if (entry != null && !entry.IsExpired())
                    {
                        _memoryCache[entry.Key] = entry;
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }
    }

    private string GetCacheFilePath(string key)
    {
        string sanitizedKey = string.Concat(key.Where(c => !Path.GetInvalidFileNameChars().Contains(c)));
        return Path.Combine(_cacheDirectory, $"{sanitizedKey}.cache");
    }

    public async Task CleanupExpiredEntriesAsync(CancellationToken cancellationToken = default)
    {
        var expiredKeys = _memoryCache.Where(kvp => kvp.Value.IsExpired()).Select(kvp => kvp.Key).ToList();
        foreach (var key in expiredKeys)
        {
            _memoryCache.TryRemove(key, out _);
        }

        await _diskLock.WaitAsync(cancellationToken);
        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*.cache");
            foreach (var file in files)
            {
                try
                {
                    string json = await File.ReadAllTextAsync(file, cancellationToken);
                    var entry = JsonSerializer.Deserialize<CacheEntry>(json);
                    if (entry != null && entry.IsExpired())
                    {
                        File.Delete(file);
                    }
                }
                catch
                {
                }
            }
        }
        finally
        {
            _diskLock.Release();
        }
    }
}

public class CacheEntry
{
    public string Key { get; set; } = string.Empty;
    public object? Value { get; set; }
    public DateTime CreatedTime { get; set; }
    public DateTime LastAccessTime { get; set; }
    public DateTime ExpirationTime { get; set; }
    public int AccessCount { get; set; }

    public bool IsExpired()
    {
        return DateTime.UtcNow > ExpirationTime;
    }
}

public class CacheStatistics
{
    public int MemoryCacheEntries { get; set; }
    public int DiskCacheEntries { get; set; }
    public long DiskCacheSizeBytes { get; set; }
    public long TotalAccessCount { get; set; }
    public string FormattedDiskCacheSize => FormatBytes(DiskCacheSizeBytes);

    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}
