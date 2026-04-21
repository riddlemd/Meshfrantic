using System.Collections.Concurrent;

namespace Meshfrantic.Services;

public class LogsService : IDisposable
{
    private const string _logFilePattern = "*.log";
    private readonly ILogger<LogsService> _logger;
    private readonly string _logsDirectory;
    private FileSystemWatcher? _watcher;
    private readonly ConcurrentDictionary<string, string> _fileCache = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheThreshold = TimeSpan.FromSeconds(1);

    public event Action? LogsChanged;
    public event Action<string>? LogFileUpdated;

    public LogsService(ILogger<LogsService> logger)
    {
        _logger = logger;
        _logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");

        if (!Directory.Exists(_logsDirectory))
        {
            Directory.CreateDirectory(_logsDirectory);
            _logger.LogInformation("Created logs directory: {LogsDirectory}", _logsDirectory);
        }

        InitializeWatcher();
    }

    private void InitializeWatcher()
    {
        try
        {
            _watcher = new FileSystemWatcher(_logsDirectory, _logFilePattern)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true,
                IncludeSubdirectories = true
            };

            _watcher.Changed += OnLogFileChanged;
            _watcher.Created += OnLogFileChanged;
            _watcher.Error += OnWatcherError;

            _logger.LogInformation("Log file watcher initialized for directory: {LogsDirectory}", _logsDirectory);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize log file watcher");
        }
    }

    private void OnLogFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            _fileCache.TryRemove(Path.GetFileName(e.FullPath), out _);

            if (DateTime.UtcNow - _lastRefresh > _cacheThreshold)
            {
                _lastRefresh = DateTime.UtcNow;
                LogsChanged?.Invoke();
                LogFileUpdated?.Invoke(Path.GetFileName(e.FullPath));
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error handling log file change: {FileName}", e.Name);
        }
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        if (e.GetException() is Exception ex)
        {
            _logger.LogError(ex, "Log file watcher error");
        }
    }

    public List<FileInfo> GetLogFiles()
    {
        try
        {
            if (!Directory.Exists(_logsDirectory))
                return [];

            var files = Directory.GetFiles(_logsDirectory, _logFilePattern, SearchOption.AllDirectories)
                .Select(f => new FileInfo(f))
                .OrderBy(f => f.Name)
                .ToList();

            _logger.LogDebug("Found {LogFileCount} log files", files.Count);
            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get log files");
            return [];
        }
    }

    public async Task<string> ReadLogFileAsync(string fileName)
    {
        try
        {
            if (_fileCache.TryGetValue(fileName, out var cached))
            {
                return cached;
            }

            var filePath = Path.Combine(_logsDirectory, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Log file not found: {FileName}", fileName);
                return string.Empty;
            }

            string content = string.Empty;
            int retries = 3;

            while (retries > 0)
            {
                try
                {
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var reader = new StreamReader(stream);
                    content = await reader.ReadToEndAsync();
                    break;
                }
                catch (IOException ex) when (retries > 1)
                {
                    retries--;
                    await Task.Delay(100);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Failed to read log file after retries: {FileName}", fileName);
                    return string.Empty;
                }
            }

            _fileCache[fileName] = content;
            _logger.LogDebug("Read log file: {FileName} ({SizeKB} KB)",
                fileName, content.Length / 1024);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read log file: {FileName}", fileName);
            return string.Empty;
        }
    }

    public async Task DeleteLogFileAsync(string fileName)
    {
        try
        {
            var filePath = Path.Combine(_logsDirectory, fileName);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Log file not found for deletion: {FileName}", fileName);
                return;
            }

            _fileCache.TryRemove(fileName, out _);
            File.Delete(filePath);
            _logger.LogInformation("Deleted log file: {FileName}", fileName);

            LogsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete log file: {FileName}", fileName);
            throw;
        }
    }

    public void ClearCache()
    {
        _fileCache.Clear();
        _logger.LogDebug("Log file cache cleared");
    }

    public void Dispose()
    {
        _watcher?.Dispose();
        _fileCache.Clear();
    }
}
