using Aoiro;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(SceneEventPoint))]
    [AddComponentMenu("场景/场景事件触发器")]
    public sealed class SceneEventTrigger : MonoBehaviour, IGameplayController, ISceneEventStateReceiver
    {
        [SerializeField, LabelText("关联事件")]
        private SceneEventPoint _point;

        [SerializeField, LabelText("关联形状")] 
        private Collider2D _collider = null;
        
        private void Reset()
        {
            _point = GetComponent<SceneEventPoint>();
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
        }

        private void Awake()
        {
            if (_point == null)
            {
                _point = GetComponent<SceneEventPoint>();
            }

            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CharacterActor.TryGet(other, out var actor) || !actor.IsPlayer)
            {
                return;
            }

            this.GetSystem<SceneEventSystem>().Trigger(_point);
        }

        public void OnSceneEventStateChanged(bool isTriggerable)
        {
            if (_collider == null)
            {
                _collider = GetComponent<Collider2D>();
            }

            _collider.enabled = isTriggerable;
        }
    }
}
