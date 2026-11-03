#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Gameplay.UI
{
    internal static class UIBindingCodeBridge
    {
        internal static Action<MonoBehaviour> Generate;
    }
}
#endif
