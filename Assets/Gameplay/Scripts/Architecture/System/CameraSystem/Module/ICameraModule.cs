using System.Threading;
using Cysharp.Threading.Tasks;

namespace Gameplay
{
    /// <summary>
    /// 由 CameraDirector 按注册顺序初始化、按逆序关闭的镜头功能模块。
    /// </summary>
    public interface ICameraModule
    {
        /// <summary>
        /// 绑定只在 Module 生命周期内有效的 CameraContext。
        /// </summary>
        protected internal void Initialize(ICameraContext context);

        protected internal UniTask ReadyAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 结束所有会话、取消回调并释放 Module 持有的资源。
        /// </summary>
        protected internal void Shutdown();
    }
}
