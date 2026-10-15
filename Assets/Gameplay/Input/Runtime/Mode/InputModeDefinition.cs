using System;
using Sirenix.OdinInspector;

namespace Gameplay
{
    [Serializable]
    public readonly struct InputModeDefinition
    {
        internal bool IsValid => (EnabledChannels != InputChannelMask.None || DisabledChannels != InputChannelMask.None)
                                 && (EnabledChannels & DisabledChannels) == InputChannelMask.None;

        [field: ShowInInspector]
        internal int Priority { get; }
        [field: ShowInInspector]
        private InputChannelMask EnabledChannels { get; }
        [field: ShowInInspector]
        private InputChannelMask DisabledChannels { get; }

        public InputModeDefinition(int priority, InputChannelMask enabledChannels, InputChannelMask disabledChannels)
        {
            if (enabledChannels == InputChannelMask.None && disabledChannels == InputChannelMask.None)
            {
                throw new ArgumentException("Input mode must enable or disable at least one channel.");
            }
            if ((enabledChannels & disabledChannels) != InputChannelMask.None)
            {
                throw new ArgumentException("The same input channel cannot be both enabled and disabled.", nameof(disabledChannels));
            }

            Priority = priority;
            EnabledChannels = enabledChannels;
            DisabledChannels = disabledChannels;
        }

        internal bool TryResolve(InputChannelMask channel, out bool enabled)
        {
            if ((DisabledChannels & channel) != 0)
            {
                enabled = false;
                return true;
            }
            if ((EnabledChannels & channel) != 0)
            {
                enabled = true;
                return true;
            }

            enabled = false;
            return false;
        }
    }
}
