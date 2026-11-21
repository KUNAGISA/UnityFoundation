using Sirenix.OdinInspector;

namespace Gameplay
{
    public enum ArtCameraCoveredPolicy : byte
    {
        [LabelText("覆盖时继续")]
        ContinueWhenCovered,

        [LabelText("覆盖时暂停")]
        PauseWhenCovered,

        [LabelText("覆盖时取消")]
        CancelWhenCovered,
    }
}
