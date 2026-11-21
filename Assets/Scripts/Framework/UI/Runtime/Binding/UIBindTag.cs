using UnityEngine;

namespace Gameplay.UI
{
    internal sealed class UIBindTag : MonoBehaviour
    {
        [SerializeField, UIBindingId]
        private int _bindingId;

        [SerializeField]
        [Tooltip("不为当前绑定生成事件监听和 partial 回调。")]
        private bool _disableEvent = false;

        public int BindingId => _bindingId;
        public bool DisableEvent => _disableEvent;
    }
}
