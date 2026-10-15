using Sirenix.OdinInspector;
using Gameplay.Sequence;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasRenderer))]
    public class AdvPortrait : MonoBehaviour
    {
        [SerializeField, LabelText("立绘图片"), ChildGameObjectsOnly, ValidateInput(nameof(_portrait), "立绘图片不能为空")] 
        private Image _portrait = null;
        
        private RectTransform _transform = null;
        
        private void Awake()
        {
            _transform = GetComponent<RectTransform>();
        }

        public void ApplyState(in AdvPortraitVisualState state)
        {
            _transform.anchoredPosition = state.Origin;
            _transform.localScale = new Vector3(state.HorizontalFlip ? -1f : 1f, state.VerticalFlip ? -1f : 1f, 1f);
            _portrait.color = state.Tint.ToColor(state.Opacity);
        }
    }
}
