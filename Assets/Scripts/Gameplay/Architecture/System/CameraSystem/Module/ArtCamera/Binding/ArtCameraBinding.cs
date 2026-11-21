using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    public enum ArtCameraBindingSlotId : byte
    {
        [LabelText("玩家")]
        Player,

        [LabelText("目标")]
        Target,

        [LabelText("锚点")]
        Anchor,
    }

    public readonly struct ArtCameraBinding
    {
        public readonly ArtCameraBindingSlotId SlotId;
        public readonly Transform Target;

        public ArtCameraBinding(ArtCameraBindingSlotId slotId, Transform target)
        {
            SlotId = slotId;
            Target = target;
        }
    }
}
