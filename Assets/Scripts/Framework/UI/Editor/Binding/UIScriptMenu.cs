using UnityEditor;

namespace Gameplay.UI.Editor
{
    internal static class UIScriptMenu
    {
        /// <summary>使用项目配置的界面模板创建脚本文件。</summary>
        [MenuItem("Assets/Create/Scripting/View Script", priority = 10000)]
        public static void CreateViewScriptFile()
        {
            var templateFilePath = UIBindingProjectSettings.GetOrCreate().GetViewScriptTemplatePath();
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templateFilePath, "View.cs");
        }

        /// <summary>使用项目配置的组件模板创建脚本文件。</summary>
        [MenuItem("Assets/Create/Scripting/Widget Script", priority = 10001)]
        public static void CreateWidgetScriptFile()
        {
            var templateFilePath = UIBindingProjectSettings.GetOrCreate().GetWidgetScriptTemplatePath();
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templateFilePath, "Widget.cs");
        }
    }
}
