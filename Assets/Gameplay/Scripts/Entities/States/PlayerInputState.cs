using UnityEngine;

namespace Gameplay
{
    public sealed class PlayerInputState
    {
        public Vector2 InputDir { get; set; } = Vector2.zero;

        public void Reset()
        {
            InputDir = Vector2.zero;
        }
    }
}
