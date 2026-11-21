using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Gameplay.UI
{
    /// <summary>
    /// 为 <see cref="UIViewHost"/> 中注册的会话创建和归还界面根实例。
    /// </summary>
    public interface IUIViewInstanceProvider
    {
        /// <summary>加载或从池中获取界面根实例。</summary>
        /// <remarks>
        /// 成功返回的根实例必须未激活，并挂在 <paramref name="parent"/> 下；返回时所有权移交请求会话。
        /// 因取消或失败而未交付的实例由 Provider 自行清理；已交付但会话已关闭的晚到实例仍由会话归还。
        /// </remarks>
        UniTask<GameObject> InstantiateAsync(string location, Transform parent, CancellationToken cancellationToken);

        /// <summary>回收已交付的根实例，<paramref name="location"/> 用作对象池资源键。</summary>
        /// <remarks>
        /// 每个成功交付的实例由请求会话恰好归还一次；归还其他 Provider 的实例或重复归还均违反所有权约定。
        /// </remarks>
        void Release(string location, GameObject instance);
    }
}
