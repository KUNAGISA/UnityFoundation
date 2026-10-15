using Sirenix.OdinInspector;

namespace Gameplay
{
    public enum ArtCameraLostTargetPolicy : byte
    {
        [LabelText("取消镜头")]
        CancelSession,

        [LabelText("保持最后姿态")]
        FreezeLastPose,

        [LabelText("清空绑定")]
        ClearBinding,
    }
}
