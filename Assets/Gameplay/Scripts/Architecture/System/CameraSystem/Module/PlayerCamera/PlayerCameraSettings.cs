using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [CreateAssetMenu(menuName = "Gameplay/Camera/Player Camera Settings")]
    public sealed class PlayerCameraSettings : ScriptableObject
    {
        [SerializeField, LabelText("镜头预制体地址")]
        private string _rigLocation = "Cameras/PlayerCameraRig";
        public string RigLocation => _rigLocation;

        [SerializeField, LabelText("优先级偏移")]
        private int _priorityOffset;
        public int PriorityOffset => _priorityOffset;

        [SerializeField, MinValue(0.01f), LabelText("瞬移判定距离")]
        private float _snapDistanceThreshold = 15f;
        public float SnapDistanceThreshold => Mathf.Max(0.01f, _snapDistanceThreshold);

        public void Validate(CameraPriorityRange range)
        {
            if (string.IsNullOrWhiteSpace(_rigLocation))
            {
                throw new System.InvalidOperationException("Player camera rig location is empty.");
            }
            if (!range.Contains(range.ResolveOffset(_priorityOffset)))
            {
                throw new System.InvalidOperationException($"Player camera priority offset [{_priorityOffset}] is outside its provider range.");
            }
        }
    }
}
