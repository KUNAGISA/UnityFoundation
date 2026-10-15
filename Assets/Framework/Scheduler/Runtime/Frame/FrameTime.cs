namespace Gameplay.Scheduler
{
    public readonly struct FrameTime
    {
        /// <summary>
        /// 自上一帧以来经过的时间（以秒为单位）。
        /// </summary>
        public readonly float DeltaTime;

        /// <summary>
        /// 自上一帧以来经过的时间（以毫秒为单位）。
        /// </summary>
        public readonly double DeltaTimeMs;

        /// <summary>
        /// 上一帧以来经过的未缩放时间（以秒为单位）。
        /// </summary>
        public readonly float UnscaledDeltaTime;

        /// <summary>
        /// 自上一帧以来经过的未缩放时间（以毫秒为单位）。
        /// </summary>
        public readonly double UnscaledDeltaTimeMs;

        /// <summary>
        /// 自启动以来经过的时间（以毫秒为单位）。
        /// </summary>
        public readonly long TimeMs;

        /// <summary>
        /// 自启动以来经过的未缩放时间（以毫秒为单位）。
        /// </summary>
        public readonly long UnscaledTimeMs;

        public FrameTime(double deltaTimeMs, double unscaledDeltaTimeMs, long timeMs, long unscaledTimeMs)
        {
            DeltaTime = (float)(deltaTimeMs * 0.001);
            DeltaTimeMs = deltaTimeMs;
            
            UnscaledDeltaTime = (float)(unscaledDeltaTimeMs * 0.001);
            UnscaledDeltaTimeMs = unscaledDeltaTimeMs;

            TimeMs = timeMs;
            UnscaledTimeMs = unscaledTimeMs;
        }
    }
}