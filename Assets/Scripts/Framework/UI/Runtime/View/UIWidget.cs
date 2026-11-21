using UnityEngine;

namespace Gameplay.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    public abstract class UIWidget : MonoBehaviour
    {
#if UNITY_EDITOR
        [ContextMenu("生成绑定代码", false, 10000)]
        private void GenerateCode()
        {
            UIBindingCodeBridge.Generate?.Invoke(this);
        }
#endif
    }
}
