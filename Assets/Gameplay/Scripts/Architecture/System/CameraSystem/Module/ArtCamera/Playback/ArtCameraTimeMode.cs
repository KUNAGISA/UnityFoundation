using Sirenix.OdinInspector;

namespace Gameplay
{
    public enum ArtCameraTimeMode : byte
    {
        [LabelText("镜头时间")]
        Camera = 0,

        [LabelText("流程时间")]
        Procedure = 1,
    }
}
