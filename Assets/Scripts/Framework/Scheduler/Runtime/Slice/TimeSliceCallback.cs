namespace Gameplay.Scheduler
{
    /// <summary>
    /// 分帧离散任务回调
    /// </summary>
    /// <param name="budget">当前任务可使用的动态时间预算</param>
    public delegate void TimeSliceCallback(in TimeSliceBudget budget);
}
