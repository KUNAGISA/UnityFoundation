using System;
using UnityEngine;

namespace Gameplay.UI
{
    public sealed class UIViewExtensionContainer
    {
        // 各阶段显式遍历以避免委托分配；Released/Shutdown 逆序，其余阶段保持注册顺序。
        private readonly UIViewExtension[] _extensions;

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
