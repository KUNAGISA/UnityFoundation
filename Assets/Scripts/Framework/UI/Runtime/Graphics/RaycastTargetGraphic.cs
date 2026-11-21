using UnityEngine;
using UnityEngine.UI;

namespace Gameplay.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class RaycastTargetGraphic : MaskableGraphic
    {
        /// <summary>不生成可见网格，仅保留 Graphic 的射线检测能力。</summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
