using System;
using Aoiro;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace Gameplay
{
    public sealed class InputService : ServiceBase, IInputService
    {
        private readonly InputChannel[] _channels;

        [ShowInInspector]
        private readonly InputModeStack _modeStack = new();
        private readonly PlayerInputChannel _playerInputChannel = new();
        private readonly UIGlobalInputChannel _uiGlobalInputChannel = new();
        private readonly UIModalInputChannel _uiModalInputChannel = new();
        private readonly InputPresentationTracker _presentationTracker = new();

        private InputControls _controls;
        private InputSystemUIInputModule _uiInputModule;

        public PlayerInputChannel Player => _playerInputChannel;
        public InputPresentation CurrentPresentation => _presentationTracker.CurrentPresentation;
        public ISignal<UIInputCommand> UICommand => _uiModalInputChannel.UICommand;
        public ISignal<InputPresentationChanged> InputPresentationChanged => _presentationTracker.Changed;

        public InputService()
        {
            _channels = new InputChannel[]
            {
                _playerInputChannel,
                new HotKeyInputChannel(),
                _uiGlobalInputChannel,
                _uiModalInputChannel,
            };
        }

        protected override void OnInitialize()
        {
            _controls = new InputControls();
            _presentationTracker.Initialize(_controls);
            foreach (var channel in _channels)
            {
                channel.Initialize(_controls);
            }
            Compute();
        }

        protected override void OnShutdown()
        {
            _modeStack.Clear();
            UnbindUIInputModule(_uiInputModule);
            _uiModalInputChannel.ClearCommands();
            _presentationTracker.Shutdown();

            foreach (var channel in _channels)
            {
                channel.Shutdown();
            }

            _controls.Disable();
            _controls.Dispose();
            _controls = null;
        }

        public InputModeToken PushMode(InputModeDefinition mode)
        {
            var entryId = _modeStack.Add(mode);
            Compute();
            return new InputModeToken(this, entryId);
        }

        public bool TryGetPointerPosition(out Vector2 position) => _presentationTracker.TryGetPointerPosition(out position);

        public UIInputModuleToken BindUIInputModule(InputSystemUIInputModule module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (_uiInputModule != null)
            {
                throw new InvalidOperationException("InputService only supports one bound UI input module at a time.");
            }

            module.actionsAsset = _controls.asset;
            try
            {
                _uiGlobalInputChannel.BindUIInputModule(module);
                _uiModalInputChannel.BindUIInputModule(module);
                _uiInputModule = module;
                return new UIInputModuleToken(this, module);
            }
            catch
            {
                _uiModalInputChannel.UnbindUIInputModule(module);
                _uiGlobalInputChannel.UnbindUIInputModule(module);
                module.actionsAsset = null;
                throw;
            }
        }

        public void Cancel(InputModeToken token)
        {
            if (!ReferenceEquals(token.Service, this) || token.EntryId == 0)
            {
                return;
            }

            if (_modeStack.Remove(token.EntryId))
            {
                Compute();
            }
        }

        public void Cancel(UIInputModuleToken token)
        {
            if (!ReferenceEquals(token.Service, this) || !ReferenceEquals(token.Module, _uiInputModule))
            {
                return;
            }

            UnbindUIInputModule(_uiInputModule);
        }

        private void UnbindUIInputModule(InputSystemUIInputModule module)
        {
            if (module == null)
            {
                return;
            }

            _uiModalInputChannel.UnbindUIInputModule(module);
            _uiGlobalInputChannel.UnbindUIInputModule(module);
            module.actionsAsset = null;
            _uiInputModule = null;
        }

        private void Compute()
        {
            foreach (var channel in _channels)
            {
                var enabled = _modeStack.Resolve(channel.Mask, channel.DefaultEnabled);
                channel.SetEnabled(enabled);
            }
        }
    }
}
