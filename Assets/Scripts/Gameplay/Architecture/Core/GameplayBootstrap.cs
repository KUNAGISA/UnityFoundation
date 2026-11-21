using UnityEngine;

namespace Gameplay
{
    internal static class GameplayBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void OnInitialize()
        {
            Application.quitting -= OnQuitting;
            Application.quitting += OnQuitting;
            
            GameplayArchitecture.Shutdown();
            GameplayArchitecture.Initialize();
        }

        private static void OnQuitting()
        {
            Application.quitting -= OnQuitting;
            
            GameplayArchitecture.Shutdown();
        }
    }
}
