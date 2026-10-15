using System;
using Sirenix.OdinInspector;
using Unity.Cinemachine;
using UnityEngine;

namespace Gameplay
{
    public sealed class PlayerCameraRig : MonoBehaviour
    {
        [SerializeField, ChildGameObjectsOnly, LabelText("跟随节点")]
        private Transform _follow;
        public Transform Follow => _follow;

        [SerializeField, ChildGameObjectsOnly, LabelText("玩家虚拟镜头")]
        private CinemachineCamera _normalCamera;
        public CinemachineCamera NormalCamera => _normalCamera;

        public void ValidateConfiguration()
        {
            if (_follow == null || _normalCamera == null)
            {
                throw new InvalidOperationException($"Player camera rig [{name}] is missing Follow or NormalCamera reference.");
            }
            if (_normalCamera.GetComponent<CinemachineFollow>() == null)
            {
                throw new InvalidOperationException($"Player camera [{_normalCamera.name}] requires CinemachineFollow.");
            }
        }

        public void Snap()
        {
            _normalCamera.PreviousStateIsValid = false;
        }
    }
}
