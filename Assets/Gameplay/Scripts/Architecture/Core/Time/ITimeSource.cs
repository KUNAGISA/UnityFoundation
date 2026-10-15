using Gameplay.Scheduler;

namespace Gameplay
{
    public interface ITimeSource
    {
        /// <summary>
        /// 未时间缩放影响的的高精度时间差（毫秒）
        /// </summary>
        double UnscaledDeltaTimeMs { get; }
        
        /// <summary>
        /// 未时间缩放影响的的时间差（秒）
        /// </summary>
        float UnscaledDeltaTime { get; }
        
        /// <summary>
        /// 受时间缩放影响的高精度时间差（毫秒）
        /// </summary>
        double DeltaTimeMs { get; }
        
        /// <summary>
        /// 受时间缩放影响的时间差（秒）
        /// </summary>
        float DeltaTime { get; }
        
        /// <summary>
        /// 全局时间缩放
        /// </summary>
        float TimeScale { get; set; }
        
        /// <summary>
        /// 受时间缩放影响的累计时间
        /// </summary>
        long TimeMs { get; }
        
        /// <summary>
        /// 未受时间缩放影响的累计时间
        /// </summary>
        long UnscaledTimeMs { get; }
    }
}