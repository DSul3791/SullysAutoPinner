using System;
using System.IO;
using System.Threading;
using BepInEx.Logging;

namespace SullysAutoPinner
{
    public class ConfigWatcher : IDisposable
    {
        private readonly string _filePath;
        private readonly ManualLogSource _logger;
        private readonly Action _onConfigChanged;
        private FileSystemWatcher _watcher;
        private System.Threading.Timer _debounceTimer;

        public ConfigWatcher(string filePath, Action onConfigChanged, ManualLogSource logger)
        {
            _filePath = filePath;
            _onConfigChanged = onConfigChanged;
            _logger = logger;

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"[ConfigWatcher] File does not exist yet: {filePath}");
                return;
            }

            var directory = Path.GetDirectoryName(filePath);
            var filename = Path.GetFileName(filePath);

            _watcher = new FileSystemWatcher(directory, filename)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnChanged;
            _watcher.Renamed += OnChanged;

            _logger.LogInfo($"[ConfigWatcher] Watching for changes: {filePath}");
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            _debounceTimer?.Dispose();

            _debounceTimer = new System.Threading.Timer(_ =>
            {
                try
                {
                    _logger.LogInfo($"[ConfigWatcher] Detected config change: {_filePath}");
                    _onConfigChanged?.Invoke();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"[ConfigWatcher] Reload failed: {ex.Message}");
                }
            }, null, 300, Timeout.Infinite);
        }

        public void Dispose()
        {
            _watcher?.Dispose();
            _debounceTimer?.Dispose();
        }
    }
}
