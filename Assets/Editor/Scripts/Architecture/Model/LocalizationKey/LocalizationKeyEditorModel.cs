using System;
using System.Collections.Generic;
using System.IO;
using Aoiro;
using Luban;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gameplay.Editor
{
    public sealed class LocalizationKeyEditorModel : ModelBase
    {
        private const string LocalizationAssetPath = "Assets/Gameplay/Tables/Data/Localization/zh/localization.bytes";
        private const double ReloadDebounceSeconds = 0.2d;

        private readonly List<LocalizationKeyOption> _options = new List<LocalizationKeyOption>();
        private readonly object _watcherLock = new object();
        private FileSystemWatcher _watcher;
        private string _watchedFullPath;
        private DateTime _lastChangeUtc;
        private bool _hasPendingChange;
        private bool _isDirty = true;

        public IReadOnlyList<LocalizationKeyOption> Options
        {
            get
            {
                EnsureLoaded();
                return _options;
            }
        }

        protected override void OnInitialize()
        {
            StartWatcher();
        }

        public bool TryGetText(string key, out string text)
        {
            EnsureLoaded();
            for (var index = 0; index < _options.Count; index++)
            {
                var option = _options[index];
                if (option.Key == key)
                {
                    text = option.Text;
                    return true;
                }
            }

            text = null;
            return false;
        }

        public void Reload()
        {
            _isDirty = true;
            EnsureLoaded();
        }

        protected override void OnShutdown()
        {
            StopWatcher();
            _options.Clear();
            _isDirty = true;
        }

        private void StartWatcher()
        {
            _watchedFullPath = Path.GetFullPath(LocalizationAssetPath);
            var directory = Path.GetDirectoryName(_watchedFullPath);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                Debug.LogWarning($"Localization table directory not found: {directory}");
                return;
            }

            _watcher = new FileSystemWatcher(directory, Path.GetFileName(_watchedFullPath))
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };

            _watcher.Changed += OnLocalizationFileChanged;
            _watcher.Created += OnLocalizationFileChanged;
            _watcher.Renamed += OnLocalizationFileRenamed;
            EditorApplication.update += OnEditorUpdate;
        }

        private void StopWatcher()
        {
            EditorApplication.update -= OnEditorUpdate;

            if (_watcher != null)
            {
                _watcher.Changed -= OnLocalizationFileChanged;
                _watcher.Created -= OnLocalizationFileChanged;
                _watcher.Renamed -= OnLocalizationFileRenamed;
                _watcher.Dispose();
                _watcher = null;
            }

            lock (_watcherLock)
            {
                _hasPendingChange = false;
            }
        }

        private void EnsureLoaded()
        {
            if (!_isDirty)
            {
                return;
            }

            _isDirty = false;
            _options.Clear();

            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(LocalizationAssetPath);
            if (asset == null)
            {
                Debug.LogWarning($"Localization table not found: {LocalizationAssetPath}");
                return;
            }

            try
            {
                var buffer = new ByteBuf(asset.bytes);
                var count = buffer.ReadSize();
                _options.Capacity = Math.Max(_options.Capacity, count);
                for (var index = 0; index < count; index++)
                {
                    var key = buffer.ReadString();
                    var text = buffer.ReadString();
                    _options.Add(new LocalizationKeyOption(key, text));
                }
            }
            catch (Exception e)
            {
                _options.Clear();
                Debug.LogError($"Failed to load localization table: {LocalizationAssetPath}\n{e}");
            }
        }

        private void OnLocalizationFileChanged(object sender, FileSystemEventArgs e)
        {
            MarkPendingChange(e.FullPath);
        }

        private void OnLocalizationFileRenamed(object sender, RenamedEventArgs e)
        {
            MarkPendingChange(e.FullPath);
        }

        private void MarkPendingChange(string fullPath)
        {
            if (!IsWatchedFile(fullPath))
            {
                return;
            }

            lock (_watcherLock)
            {
                _lastChangeUtc = DateTime.UtcNow;
                _hasPendingChange = true;
            }
        }

        private bool IsWatchedFile(string fullPath)
        {
            return string.Equals(Path.GetFullPath(fullPath), _watchedFullPath, StringComparison.OrdinalIgnoreCase);
        }

        private void OnEditorUpdate()
        {
            var shouldReload = false;
            lock (_watcherLock)
            {
                if (_hasPendingChange && (DateTime.UtcNow - _lastChangeUtc).TotalSeconds >= ReloadDebounceSeconds)
                {
                    _hasPendingChange = false;
                    shouldReload = true;
                }
            }

            if (!shouldReload)
            {
                return;
            }

            AssetDatabase.ImportAsset(LocalizationAssetPath, ImportAssetOptions.ForceUpdate);
            Reload();
            InternalEditorUtility.RepaintAllViews();
            Debug.Log("Localization table changed. Reloaded.");
        }
    }
}
