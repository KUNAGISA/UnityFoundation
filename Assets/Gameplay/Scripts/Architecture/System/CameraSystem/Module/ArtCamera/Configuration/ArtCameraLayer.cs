using Sirenix.OdinInspector;

namespace Gameplay
{
    public enum ArtCameraLayer : byte
    {
        [LabelText("环境层")]
        Ambient,

        [LabelText("交互层")]
        Interaction,

        [LabelText("主演出层")]
        MainSequence,

        [LabelText("关键演出层")]
        CriticalCutscene,
    }
}
