namespace Gameplay
{
    public sealed class GameTimeSource : ITimeSource
    {
        public double UnscaledDeltaTimeMs { get; private set; }
        public float UnscaledDeltaTime { get; private set; }
        public double DeltaTimeMs { get; private set; }
        public float DeltaTime { get; private set; }
        public float TimeScale { get; set; } = 1f;
        public long TimeMs { get; private set; }
        public long UnscaledTimeMs { get; private set; }

        public void Reset()
        {
            UnscaledDeltaTimeMs = DeltaTimeMs = 0d;
            UnscaledDeltaTime = DeltaTime = 0f;
            TimeScale = 1f;
            TimeMs = UnscaledTimeMs = 0L;
        }
        
        public void Evaluate(float deltaTime)
        {
            Evaluate(deltaTime, deltaTime);
        }

        public void Evaluate(float deltaTime, float unscaledDeltaTime)
        {
            UnscaledDeltaTime = unscaledDeltaTime;
            UnscaledDeltaTimeMs = unscaledDeltaTime * 1000d;
            
            DeltaTime = deltaTime * TimeScale;
            DeltaTimeMs = DeltaTime * 1000d;
            
            TimeMs += (long)DeltaTimeMs;
            UnscaledTimeMs += (long)UnscaledDeltaTimeMs;
        }
    }
}
