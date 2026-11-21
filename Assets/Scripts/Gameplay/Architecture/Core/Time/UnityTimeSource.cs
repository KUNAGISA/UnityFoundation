using UnityEngine;

namespace Gameplay
{
    public sealed class UnityTimeSource : ITimeSource
    {
        public double UnscaledDeltaTimeMs => Time.unscaledDeltaTime * 1000d;
        public float UnscaledDeltaTime => Time.unscaledDeltaTime;
        public double DeltaTimeMs => Time.deltaTime * 1000d;
        public float DeltaTime => Time.deltaTime;
        public float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = value;
        }
        public long TimeMs => (long)(Time.timeAsDouble * 1000d);
        public long UnscaledTimeMs => (long)(Time.unscaledTime * 1000d);
    }
}