#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Aoiro;
using UnityEditor;
using UnityEngine;

namespace Gameplay.Tables
{
    public sealed class TableFileWatchSystem : SystemBase
    {
        private const string TableDirectory = "Assets/Gameplay/Tables/Data/Config";
        private const double ReloadDebounceSeconds = 0.2d;

        private readonly object _pendingLock = new object();
        private readonly Dictionary<string, DateTime> _pendingTables = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _readyTables = new List<string>();
        private FileSystemWatcher _watcher = null;
        private TablesModel _tables = null;
        private DateTime _reloadAllRequestedUtc;
        private bool _reloadAllRequested;

        protected override void OnInitialize()
        {
            _tables = this.GetModel<TablesModel>();
            StartWatcher();
        }

        protected override void OnShutdown()
        {
            StopWatcher();
            _tables = null;
        }

        public void Tick()
        {
            CollectReadyTables();
            for (var index = 0; index < _readyTables.Count; index++)
            {
                ReloadTable(_readyTables[index]);
            }
            _readyTables.Clear();
        }

        private void StartWatcher()
        {
            var fullPath = Path.GetFullPath(TableDirectory);
            if (!Directory.Exists(fullPath))
            {
                Debug.LogWarning($"Table directory not found: {fullPath}");
                return;
            }

            _watcher = new FileSystemWatcher(fullPath, "*.bytes")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };
            _watcher.Changed += OnTableFileChanged;
            _watcher.Created += OnTableFileChanged;
            _watcher.Renamed += OnTableFileRenamed;
            _watcher.Error += OnWatcherError;
        }

        private void StopWatcher()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
                _watcher.Changed -= OnTableFileChanged;
                _watcher.Created -= OnTableFileChanged;
                _watcher.Renamed -= OnTableFileRenamed;
                _watcher.Error -= OnWatcherError;
                _watcher.Dispose();
                _watcher = null;
            }

            lock (_pendingLock)
            {
                _pendingTables.Clear();
                _reloadAllRequested = false;
            }
            _readyTables.Clear();
        }

        private void OnTableFileChanged(object sender, FileSystemEventArgs evt)
        {
            MarkPending(evt.FullPath);
        }

        private void OnTableFileRenamed(object sender, RenamedEventArgs evt)
        {
            MarkPending(evt.FullPath);
        }

        private void OnWatcherError(object sender, ErrorEventArgs evt)
        {
            lock (_pendingLock)
            {
                _reloadAllRequestedUtc = DateTime.UtcNow;
                _reloadAllRequested = true;
            }
        }

        private void MarkPending(string fullPath)
        {
            if (!string.Equals(Path.GetExtension(fullPath), ".bytes", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var tableName = Path.GetFileNameWithoutExtension(fullPath);
            if (string.IsNullOrEmpty(tableName))
            {
                return;
            }

            lock (_pendingLock)
            {
                _pendingTables[tableName] = DateTime.UtcNow;
            }
        }

        private void CollectReadyTables()
        {
            var now = DateTime.UtcNow;
            var shouldReloadAll = false;
            lock (_pendingLock)
            {
                foreach (var pair in _pendingTables)
                {
                    if ((now - pair.Value).TotalSeconds >= ReloadDebounceSeconds)
                    {
                        _readyTables.Add(pair.Key);
                    }
                }

                for (var index = 0; index < _readyTables.Count; index++)
                {
                    _pendingTables.Remove(_readyTables[index]);
                }

                if (_reloadAllRequested && (now - _reloadAllRequestedUtc).TotalSeconds >= ReloadDebounceSeconds)
                {
                    _reloadAllRequested = false;
                    shouldReloadAll = true;
                }
            }

            if (shouldReloadAll)
            {
                CollectAllTables();
            }
        }

        private void CollectAllTables()
        {
            try
            {
                var files = Directory.GetFiles(Path.GetFullPath(TableDirectory), "*.bytes", SearchOption.TopDirectoryOnly);
                for (var index = 0; index < files.Length; index++)
                {
                    var tableName = Path.GetFileNameWithoutExtension(files[index]);
                    if (!_readyTables.Contains(tableName))
                    {
                        _readyTables.Add(tableName);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void ReloadTable(string tableName)
        {
            var normalizedTableName = tableName.ToLowerInvariant();
            var assetPath = $"{TableDirectory}/{normalizedTableName}.bytes";
            try
            {
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                _tables.Reload(normalizedTableName);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Failed to reload table: {assetPath}\n{exception}");
            }
        }
    }
}
#endif
