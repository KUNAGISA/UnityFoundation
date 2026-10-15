using UnityEditor;
using UnityEngine;

namespace Gameplay.UI.Editor
{
    [InitializeOnLoad]
    internal static class UIBindingEditorBridge
    {
        static UIBindingEditorBridge()
        {
            UIBindingCodeBridge.Generate = GenerateBindCode;
        }

        private static void GenerateBindCode(MonoBehaviour behaviour)
        {
            new UIBindingCodeGenerator().Generate(behaviour);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var script = MonoScript.FromMonoBehaviour(behaviour);
            AssetDatabase.OpenAsset(script);
        }
    }
}
