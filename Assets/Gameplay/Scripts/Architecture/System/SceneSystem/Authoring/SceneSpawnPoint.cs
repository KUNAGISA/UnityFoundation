using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [AddComponentMenu("场景/出生点")]
    public sealed class SceneSpawnPoint : MonoBehaviour
    {
        [SerializeField, LabelText("出生点Id")] 
        private int _spawnPointId;
        
        [SerializeField, LabelText("默认出生点")] 
        private bool _isDefault;
        
        [SerializeField, LabelText("绑定路径"), SceneRouteId] 
        private int _routeId;

        public int SpawnPointId => _spawnPointId;
        public bool IsDefault => _isDefault;
        public int RouteId => _routeId;
    }
}
