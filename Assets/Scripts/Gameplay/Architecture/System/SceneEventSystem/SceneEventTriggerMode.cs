using UnityEngine;

namespace Gameplay
{
    public enum SceneEventTriggerMode
    {
        [InspectorName("自动触发")]
        Auto = 0,
        
        [InspectorName("进入触发器")]
        EnterTrigger = 1,
    }
}
