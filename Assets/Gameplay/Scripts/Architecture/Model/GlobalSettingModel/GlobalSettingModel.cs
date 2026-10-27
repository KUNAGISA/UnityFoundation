using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Storage;
using UnityEngine;

namespace Gameplay
{
    public sealed class GlobalSettingModel : ModelBase
    {
        [Serializable]
        private sealed class SettingsFile
        {
            public string language;
        }

        private const string SettingsPath = "Settings/global.json";

        public GameLanguage Language { get; private set; } = GameLanguage.Chinese;

        private IStorageService _storageService;
        private bool _isDirty;

        protected override void OnInitialize()
        {
            _storageService = this.GetService<IStorageService>();
            Language = GameLanguage.Chinese;
            _isDirty = false;
        }

        protected override void OnShutdown()
        {
            try
            {
                SaveIfDirty();
            }
            finally
            {
                _storageService = null;
                Language = GameLanguage.Chinese;
                _isDirty = false;
            }
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            Language = GameLanguage.Chinese;
            _isDirty = false;

            if (!_storageService.Exists(SettingsPath))
            {
                return;
            }

            var json = await _storageService.ReadTextAsync(SettingsPath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var data = JsonUtility.FromJson<SettingsFile>(json);
                if (data == null || !GameLanguageCode.TryParse(data.language, out var language))
                {
                    Debug.LogWarning($"[Settings] Global settings [{SettingsPath}] contains an unsupported language. Falling back to zh.");
                }
                else
                {
                    Language = language;
                }
            }
            catch (ArgumentException exception)
            {
                Debug.LogWarning($"[Settings] Global settings [{SettingsPath}] is invalid JSON. Falling back to zh. {exception.Message}");
            }
        }

        internal void SetLanguage(GameLanguage language)
        {
            if (Language == language)
            {
                return;
            }

            Language = language;
            _isDirty = true;
        }

        public void SaveIfDirty()
        {
            if (!_isDirty)
            {
                return;
            }

            var json = JsonUtility.ToJson(new SettingsFile
            {
                language = Language.GetCode(),
            }, true);
            _storageService.WriteText(SettingsPath, json, StorageWriteMode.Atomic);
            _isDirty = false;
        }
    }
}
