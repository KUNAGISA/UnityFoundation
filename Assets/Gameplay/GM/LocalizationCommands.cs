using System;
using System.Threading.Tasks;
using Aoiro;
using Cysharp.Threading.Tasks;
using QFSW.QC;
using QFSW.QC.Suggestors.Tags;
using UnityEngine.Scripting;

namespace Gameplay.GM
{
    [CommandPrefix("localization.")]
    internal static class LocalizationCommands
    {
        [Preserve]
        [Command("language", "切换语言，可选值为 zh 或 en")]
        private static Task SetLanguage([Suggestions("zh", "en")] string languageCode)
        {
            if (!GameLanguageCode.TryParse(languageCode, out var language))
            {
                throw new ArgumentException($"Unsupported language [{languageCode}]. Expected zh or en.", nameof(languageCode));
            }

            return GmContext.Shared.ExecuteCommand<UniTask>(new SetLanguageCommand(language)).AsTask();
        }
    }
}
