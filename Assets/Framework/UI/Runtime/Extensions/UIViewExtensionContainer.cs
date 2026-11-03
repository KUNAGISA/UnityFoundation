using System;
using UnityEngine;

namespace Gameplay.UI
{
    public sealed class UIViewExtensionContainer
    {
        // 各阶段显式遍历以避免委托分配；Released/Shutdown 逆序，其余阶段保持注册顺序。
        private readonly UIViewExtension[] _extensions;

        /// <summary>复制扩展列表并保留注册顺序；打开与显隐按正序通知，释放与 Shutdown 按反序通知。</summary>
        public UIViewExtensionContainer(params UIViewExtension[] extensions)
        {
            if (extensions == null)
            {
                throw new ArgumentNullException(nameof(extensions));
            }

            _extensions = new UIViewExtension[extensions.Length];
            for (var index = 0; index < extensions.Length; index++)
            {
                _extensions[index] = extensions[index] ?? throw new ArgumentException($"UI view extension at index [{index}] is null.", nameof(extensions));
            }
        }

        /// <summary>在会话创建前按序校验；异常直接交给打开调用方，允许扩展拒绝打开。</summary>
        internal void BeforeOpen(in UIViewExtensionContext context)
        {
            for (var index = 0; index < _extensions.Length; index++)
            {
                _extensions[index].OnBeforeOpen(in context);
            }
        }

        internal void Refreshed(in UIViewExtensionContext context)
        {
            for (var index = 0; index < _extensions.Length; index++)
            {
                try
                {
                    _extensions[index].OnRefreshed(in context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        internal void BeforeShow(in UIViewExtensionContext context)
        {
            for (var index = 0; index < _extensions.Length; index++)
            {
                try
                {
                    _extensions[index].OnBeforeShow(in context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        internal void ShowCompleted(in UIViewExtensionContext context)
        {
            for (var index = 0; index < _extensions.Length; index++)
            {
                try
                {
                    _extensions[index].OnShowCompleted(in context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        internal void BeforeHide(in UIViewExtensionContext context)
        {
            for (var index = 0; index < _extensions.Length; index++)
            {
                try
                {
                    _extensions[index].OnBeforeHide(in context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        /// <summary>按注册反序释放会话扩展资源；逐个记录异常，确保其余扩展仍可清理。</summary>
        internal void Released(in UIViewExtensionContext context)
        {
            for (var index = _extensions.Length - 1; index >= 0; index--)
            {
                try
                {
                    _extensions[index].OnReleased(in context);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        /// <summary>所有会话注销后按注册反序清理共享扩展；单个异常不阻断后续清理。</summary>
        internal void Shutdown(UIViewHost host)
        {
            for (var index = _extensions.Length - 1; index >= 0; index--)
            {
                try
                {
                    _extensions[index].OnShutdown(host);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

    }
}
