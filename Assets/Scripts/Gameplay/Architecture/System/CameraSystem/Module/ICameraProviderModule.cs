namespace Gameplay
{
    /// <summary>
    /// 拥有顶层镜头输出的 Module；ProviderId 在整个注册生命周期内必须固定且唯一。
    /// </summary>
    public interface ICameraProviderModule : ICameraModule
    {
        CameraProviderId ProviderId { get; }
    }
}
