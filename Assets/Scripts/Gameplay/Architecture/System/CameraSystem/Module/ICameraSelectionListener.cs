namespace Gameplay
{
    /// <summary>
    /// 接收固定优先级仲裁结果变化；通知发生在 Brain 更新前的两阶段 Selected 解析中。
    /// </summary>
    internal interface ICameraSelectionListener
    {
        void OnCameraSelectionChanged(in CameraSelection selection);
    }
}
