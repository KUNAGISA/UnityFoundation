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
        public bool IsLoaded { get; private set; }

        private IStorageService _storageService;

        protected override void OnInitialize()
        {
            _storageService = this.GetService<IStorageService>();
            Language = GameLanguage.Chinese;
            IsLoaded = false;
        }

        protected override void OnShutdown()
        {
            _storageService = null;
            Language = GameLanguage.Chinese;
            IsLoaded = false;
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            Language = GameLanguage.Chinese;
            IsLoaded = false;

            if (!_storageService.Exists(SettingsPath))
            {
                IsLoaded = true;
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

            IsLoaded = true;
        }

        internal async UniTask SetLanguageAsync(GameLanguage language, CancellationToken cancellationToken)
        {
            var json = JsonUtility.ToJson(new SettingsFile
            {
                language = language.GetCode(),
            }, true);
            await _storageService.WriteTextAsync(SettingsPath, json, StorageWriteMode.Atomic, cancellationToken);
            Language = language;
        }
    }
}
