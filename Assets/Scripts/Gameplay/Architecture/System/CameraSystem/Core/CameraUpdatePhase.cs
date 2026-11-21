using Sirenix.OdinInspector;

namespace Gameplay
{
    public enum CameraUpdatePhase : byte
    {
        [LabelText("帧更新")]
        FrameTick,

        [LabelText("帧后更新")]
        LateTick,
    }
}
