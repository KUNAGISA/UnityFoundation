using UnityEditor;

namespace Gameplay.UI.Editor
{
    internal static class UIScriptMenu
    {
        [MenuItem("Assets/Create/Scripting/View Script", priority = 10000)]
        public static void CreateViewScriptFile()
        {
            var templateFilePath = UIBindingProjectSettings.GetOrCreate().GetViewScriptTemplatePath();
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templateFilePath, "View.cs");
        }

        [MenuItem("Assets/Create/Scripting/Widget Script", priority = 10001)]
        public static void CreateWidgetScriptFile()
        {
            var templateFilePath = UIBindingProjectSettings.GetOrCreate().GetWidgetScriptTemplatePath();
            ProjectWindowUtil.CreateScriptAssetFromTemplateFile(templateFilePath, "Widget.cs");
        }
    }
}
