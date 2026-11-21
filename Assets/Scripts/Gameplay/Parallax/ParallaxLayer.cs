using Aoiro;
using Gameplay.Scheduler;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    public class ParallaxLayer : MonoBehaviour, IGameplayController
    {
        [SerializeField, LabelText("视差配置"), InlineEditor(Expanded = true)]
        private ParallaxLayerProfile _profile = null;

        [SerializeField, LabelText("镜头原点")]
        private Vector3 _cameraOrigin = Vector3.zero;
        
        private Vector3 _origin = Vector3.zero;
        private CameraSystem _cameraSystem = null;
        private FrameTickToken _afterCameraUpdateToken = default;

        private void Awake()
        {
            _origin = transform.position;
            _cameraSystem = this.GetSystem<CameraSystem>();
        }

        private void OnDestroy()
        {
            _cameraSystem = null;
        }

        private void OnEnable()
        {
            _afterCameraUpdateToken = _cameraSystem.AddAfterCameraUpdate(OnAfterCameraUpdate);
        }

        private void OnDisable()
        {
            _afterCameraUpdateToken.Cancel();
            _afterCameraUpdateToken = default;
        }

        private void OnAfterCameraUpdate(in FrameTime time)
        {
            var mainCamera = _cameraSystem.MainCamera;
            if (mainCamera == null)
            {
                return;
            }

            var follow = Vector2.one - _profile.Factor;
            var delta = (mainCamera.transform.position - _cameraOrigin) * follow;
            transform.position = _origin + new Vector3(delta.x, delta.y);
        }
    }
}
