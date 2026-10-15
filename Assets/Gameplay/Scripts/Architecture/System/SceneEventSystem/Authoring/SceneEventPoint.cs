using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [AddComponentMenu("场景/场景事件")]
    public sealed class SceneEventPoint : MonoBehaviour
    {
        [SerializeField, LabelText("事件Id"), SceneEventId, TitleGroup("事件配置")]
        private int _eventId;

        [SerializeField, LabelText("触发方式")]
        private SceneEventTriggerMode _triggerMode;

        [SerializeField, TitleGroup("路径配置")]
        private SceneRoute _route;

        [SerializeField, LabelText("表现对象"), TitleGroup("关联表现"), SceneObjectsOnly]
        private GameObject _visualTarget;

        [SerializeField, LabelText("可触发时显示"), ShowIf("_visualTarget")]
        private bool _visibleWhenTriggerable = true;

        private ISceneEventStateReceiver _stateReceiver;

        public int EventId => _eventId;
        public SceneEventTriggerMode TriggerMode => _triggerMode;
        public SceneRoute Route => _route;

        private void Awake()
        {
            _stateReceiver = GetComponentInChildren<ISceneEventStateReceiver>(true);
        }

        public void NotifyStateChanged(bool isTriggerable)
        {
            if (_visualTarget != null)
            {
                _visualTarget.SetActive(isTriggerable == _visibleWhenTriggerable);
            }

            _stateReceiver?.OnSceneEventStateChanged(isTriggerable);
        }
    }
}
