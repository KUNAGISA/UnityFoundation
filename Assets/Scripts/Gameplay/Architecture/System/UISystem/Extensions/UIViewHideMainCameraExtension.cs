using System;
using System.Collections.Generic;
using Gameplay.UI;

namespace Gameplay
{
    internal sealed class UIViewHideMainCameraExtension : UIViewExtension
    {
        private readonly CameraSystem _cameraSystem;
        private readonly Dictionary<UIViewToken, MainCameraHideToken> _hideTokensByView = new();

        internal UIViewHideMainCameraExtension(CameraSystem cameraSystem)
        {
            _cameraSystem = cameraSystem ?? throw new ArgumentNullException(nameof(cameraSystem));
        }

        protected override void OnShowCompleted(in UIViewExtensionContext context)
        {
            if (context.HasOption(UIViewOptionFlags.HideMainCamera))
            {
                AcquireOwnership(context.Token);
            }
        }

        protected override void OnBeforeHide(in UIViewExtensionContext context)
        {
            ReleaseOwnership(context.Token);
        }

        protected override void OnReleased(in UIViewExtensionContext context)
        {
            ReleaseOwnership(context.Token);
        }

        protected override void OnShutdown(UIViewHost host)
        {
            foreach (var hideToken in _hideTokensByView.Values)
            {
                hideToken.Cancel();
            }

            _hideTokensByView.Clear();
        }

        private void AcquireOwnership(UIViewToken token)
        {
            if (!_hideTokensByView.ContainsKey(token))
            {
                _hideTokensByView.Add(token, _cameraSystem.HideMainCamera());
            }
        }

        private void ReleaseOwnership(UIViewToken token)
        {
            if (_hideTokensByView.Remove(token, out var hideToken))
            {
                hideToken.Cancel();
            }
        }
    }
}
