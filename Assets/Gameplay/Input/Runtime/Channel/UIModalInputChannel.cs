using System;
using Aoiro;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

namespace Gameplay
{
    internal sealed class UIModalInputChannel : InputChannel, InputControls.IUIModalActions
    {
        private readonly Signal<UIInputCommand> _uiCommand = new();
        private InputSystemUIInputModule _uiInputModule;
        private InputActionReference _move;
        private InputActionReference _submit;
        private InputActionReference _scrollWheel;

        internal ISignal<UIInputCommand> UICommand => _uiCommand;

        internal UIModalInputChannel() : base(InputChannelMask.UIModal, false)
        {
        }

        internal void ClearCommands() => _uiCommand.Clear();

        internal void BindUIInputModule(InputSystemUIInputModule module)
        {
            if (_uiInputModule != null)
            {
                throw new InvalidOperationException("A UI input module is already bound to the UI modal input channel.");
            }

            _uiInputModule = module ?? throw new ArgumentNullException(nameof(module));
            ApplyUIInputModuleBindings(IsEnabled);
        }

        internal void UnbindUIInputModule(InputSystemUIInputModule module)
        {
            if (!ReferenceEquals(_uiInputModule, module))
            {
                return;
            }

            ApplyUIInputModuleBindings(false);
            _uiInputModule = null;
        }

        protected override InputActionMap ResolveActionMap(InputControls controls)
        {
            return controls.UIModal;
        }

        protected override void OnInitialize(InputControls controls)
        {
            controls.UIModal.AddCallbacks(this);
            _move = InputActionReference.Create(controls.UIModal.Navigate);
            _submit = InputActionReference.Create(controls.UIModal.Submit);
            _scrollWheel = InputActionReference.Create(controls.UIModal.ScrollWheel);
        }

        protected override void OnEnabledChanged(bool enabled) => ApplyUIInputModuleBindings(enabled);

        protected override void OnShutdown()
        {
            ApplyUIInputModuleBindings(false);
            _uiInputModule = null;
            DestroyReference(ref _move);
            DestroyReference(ref _submit);
            DestroyReference(ref _scrollWheel);
        }

        public void OnNavigate(InputAction.CallbackContext context) { }
        public void OnScrollWheel(InputAction.CallbackContext context) { }
        public void OnSubmit(InputAction.CallbackContext context) { }
        public void OnCancel(InputAction.CallbackContext context) => EmitPerformed(context, UIInputCommand.Cancel);
        public void OnContextNorth(InputAction.CallbackContext context) => EmitPerformed(context, UIInputCommand.ContextNorth);
        public void OnContextWest(InputAction.CallbackContext context) => EmitPerformed(context, UIInputCommand.ContextWest);
        public void OnPageLeft(InputAction.CallbackContext context) => EmitPerformed(context, UIInputCommand.PageLeft);
        public void OnPageRight(InputAction.CallbackContext context) => EmitPerformed(context, UIInputCommand.PageRight);

        private void EmitPerformed(InputAction.CallbackContext context, UIInputCommand command)
        {
            if (context.performed)
            {
                _uiCommand.Emit(command);
            }
        }

        private void ApplyUIInputModuleBindings(bool enabled)
        {
            if (_uiInputModule == null)
            {
                return;
            }

            _uiInputModule.move = enabled ? _move : null;
            _uiInputModule.submit = enabled ? _submit : null;
            _uiInputModule.scrollWheel = enabled ? _scrollWheel : null;
            _uiInputModule.cancel = null;
        }

        private static void DestroyReference(ref InputActionReference reference)
        {
            if (reference != null)
            {
                UnityEngine.Object.Destroy(reference);
                reference = null;
            }
        }
    }
}
