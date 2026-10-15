using Sirenix.OdinInspector;

namespace Gameplay
{
    public enum ArtCameraCompletionPolicy : byte
    {
        [LabelText("完成后释放")]
        ReleaseOnComplete,

        [LabelText("保持末帧")]
        HoldLastFrame,

        [LabelText("循环至取消")]
        LoopUntilCancelled,
    }
}
