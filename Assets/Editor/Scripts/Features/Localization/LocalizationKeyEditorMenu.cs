using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Gameplay.Editor
{
    internal static class LocalizationKeyEditorMenu
    {
        [MenuItem("Tools/Gameplay/Reload Localization Table")]
        private static void ReloadLocalizationTable()
        {
            EditorArchitecture.Instance.GetModule<LocalizationKeyEditorModel>().Reload();
            InternalEditorUtility.RepaintAllViews();
            Debug.Log("Localization table reloaded.");
        }
    }
}
