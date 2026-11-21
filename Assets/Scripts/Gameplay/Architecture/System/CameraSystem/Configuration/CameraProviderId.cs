using Sirenix.OdinInspector;

namespace Gameplay
{
    public enum CameraProviderId : byte
    {
        [LabelText("无")]
        None,

        [LabelText("玩家镜头")]
        Player,
        
        [LabelText("美术镜头")]
        Art,

        [LabelText("调试镜头")]
        Debug,
    }
}
