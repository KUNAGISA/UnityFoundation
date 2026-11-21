using System;

namespace Gameplay
{
    public sealed class ItemActivation
    {
        private readonly Action<int> _action;

        public int ActivatedIndex { get; private set; } = -1;

        public ItemActivation(Action<int> action)
        {
            _action = action ?? throw new ArgumentNullException(nameof(action));
        }

        public void Activate(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            ActivatedIndex = index;
            _action(index);
        }

        public bool Clear()
        {
            if (ActivatedIndex < 0)
            {
                return false;
            }

            ActivatedIndex = -1;
            return true;
        }

        public bool Validate(int itemCount)
        {
            if (itemCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(itemCount));
            }

            if (ActivatedIndex < itemCount)
            {
                return false;
            }

            return Clear();
        }
    }
}
