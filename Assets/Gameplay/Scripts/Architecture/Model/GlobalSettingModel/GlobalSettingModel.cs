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

        protected override void OnInitialize()
        {
            _storageService = this.GetService<IStorageService>();
            Language = GameLanguage.Chinese;
        }

        protected override void OnShutdown()
        {
            _storageService = null;
            Language = GameLanguage.Chinese;
        }

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            Language = GameLanguage.Chinese;

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
            var json = JsonUtility.ToJson(new SettingsFile
            {
                language = language.GetCode(),
            }, true);
            _storageService.WriteText(SettingsPath, json, StorageWriteMode.Atomic);
            Language = language;
        }
    }
}
