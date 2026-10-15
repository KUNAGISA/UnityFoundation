using Unity.Cinemachine;

namespace Gameplay
{
    /// <summary>
    /// 为同一 Provider 的两个 Session 解析内部 Blend；跨 Provider Blend 由 CameraSystemSettings 决定。
    /// </summary>
    internal interface ICameraProviderBlendResolver
    {
        bool TryResolveBlend(long fromSessionId, long toSessionId, out CinemachineBlendDefinition blend);
    }
}
