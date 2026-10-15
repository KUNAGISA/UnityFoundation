using UnityEngine;
using UnityEngine.InputSystem;

namespace Gameplay
{
    public sealed class PlayerInputChannel : InputChannel, InputControls.IPlayerActions
    {
        public Vector2 Move { get; private set; } = Vector2.zero;

        public InputButtonState Attack { get; private set; }
        public InputButtonState Interact { get; private set; }
        public InputButtonState Crouch { get; private set; }
        public InputButtonState Jump { get; private set; }

        internal PlayerInputChannel() : base(InputChannelMask.Player, false)
        {
        }

        public void ResetTransient()
        {
            Attack = Attack.WithoutTransient();
            Interact = Interact.WithoutTransient();
            Crouch = Crouch.WithoutTransient();
            Jump = Jump.WithoutTransient();
        }

        protected override InputActionMap ResolveActionMap(InputControls controls)
        {
            return controls.Player;
        }

        protected override void OnInitialize(InputControls controls)
        {
            controls.Player.AddCallbacks(this);
        }

        protected override void OnEnabledChanged(bool enabled)
        {
            if (!enabled)
            {
                ResetState();
            }
        }

        void InputControls.IPlayerActions.OnMove(InputAction.CallbackContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            Move = context.ReadValue<Vector2>().normalized;
        }

        void InputControls.IPlayerActions.OnAttack(InputAction.CallbackContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (context.performed)
            {
                Attack = Attack.WithPressed();
            }
            else if (context.canceled)
            {
                Attack = Attack.WithReleased();
            }
        }

        void InputControls.IPlayerActions.OnInteract(InputAction.CallbackContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (context.performed)
            {
                Interact = Interact.WithPressed();
            }
            else if (context.canceled)
            {
                Interact = Interact.WithReleased();
            }
        }

        void InputControls.IPlayerActions.OnCrouch(InputAction.CallbackContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (context.performed)
            {
                Crouch = Crouch.WithPressed();
            }
            else if (context.canceled)
            {
                Crouch = Crouch.WithReleased();
            }
        }

        void InputControls.IPlayerActions.OnJump(InputAction.CallbackContext context)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (context.performed)
            {
                Jump = Jump.WithPressed();
            }
            else if (context.canceled)
            {
                Jump = Jump.WithReleased();
            }
        }

        private void ResetState()
        {
            Move = Vector2.zero;
            Attack = default;
            Interact = default;
            Crouch = default;
            Jump = default;
        }
    }
}
