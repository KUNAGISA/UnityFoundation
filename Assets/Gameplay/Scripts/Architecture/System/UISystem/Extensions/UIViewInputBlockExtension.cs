using System;
using System.Collections.Generic;
using Gameplay.UI;

namespace Gameplay
{
    internal sealed class UIViewInputBlockExtension : UIViewExtension
    {
        private readonly IInputService _inputService;
        private readonly HashSet<UIViewToken> _owners = new();
        private InputModeToken _inputModeToken;

        internal UIViewInputBlockExtension(IInputService inputService)
        {
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        }

        protected override void OnBeforeShow(in UIViewExtensionContext context)
        {
            if (!context.HasOption(UIViewOptionFlags.BlockGameplayInput))
            {
                return;
            }

            AddOwner(context.Token);
        }

        protected override void OnBeforeHide(in UIViewExtensionContext context)
        {
            RemoveOwner(context.Token);
        }

        protected override void OnReleased(in UIViewExtensionContext context)
        {
            RemoveOwner(context.Token);
        }

        protected override void OnShutdown(UIViewHost host)
        {
            _owners.Clear();
            CancelInputMode();
        }

        private void AddOwner(UIViewToken token)
        {
            if (_owners.Add(token) && _owners.Count == 1)
            {
                _inputModeToken = _inputService.PushMode(InputModeCatalog.View);
            }
        }

        private void RemoveOwner(UIViewToken token)
        {
            if (_owners.Remove(token) && _owners.Count == 0)
            {
                CancelInputMode();
            }
        }

        private void CancelInputMode()
        {
            _inputModeToken.Cancel();
            _inputModeToken = default;
        }
    }
}
