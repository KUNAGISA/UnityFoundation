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

        /// <summary>从运行时组件的编辑器菜单进入生成器；写入后刷新资源数据库并定位到对应脚本。</summary>
        private static void GenerateBindCode(MonoBehaviour behaviour)
        {
            new UIBindingCodeGenerator().Generate(behaviour);
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            var script = MonoScript.FromMonoBehaviour(behaviour);
            AssetDatabase.OpenAsset(script);
        }
    }
}
