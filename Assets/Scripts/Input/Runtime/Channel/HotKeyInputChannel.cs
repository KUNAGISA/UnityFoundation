namespace Gameplay
{
    internal sealed class HotKeyInputChannel : InputChannel
    {
        internal HotKeyInputChannel() : base(InputChannelMask.HotKey, false)
        {
        }

        protected override UnityEngine.InputSystem.InputActionMap ResolveActionMap(InputControls controls)
        {
            return controls.HotKey;
        }
    }
}
