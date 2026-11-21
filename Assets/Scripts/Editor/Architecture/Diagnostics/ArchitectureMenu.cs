using UnityEditor;

namespace Gameplay.Editor
{
    internal static class ArchitectureMenu
    {
        [MenuItem("Window/Architecture/Gameplay")]
        private static void OpenGameplayArchitectureWindow()
        {
            var window = EditorWindow.GetWindow<GameplayArchitectureWindow>(false, "Gameplay Architecture");
            window.Show();
        }
    }
}
