using System;
using System.Threading;

namespace Gameplay
{
    public class Progress : IProgressReporter, IReadOnlyProgress
    {
        private float _value = 0f;
        public float Value => Volatile.Read(ref _value);

        public void Report(float value)
        {
            value = Math.Clamp(value, 0f, 1f);
            Volatile.Write(ref _value, value);
        }
    }
}