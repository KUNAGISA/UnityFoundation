using System;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Gameplay.Scheduler
{
    /// <summary>
    /// 分帧离散调度器
    /// </summary>
    public sealed class TimeSliceScheduler
    {
        private const int InvalidIndex = -1;

        private readonly int _budgetUs;
        private readonly long _budgetTicks;
        private readonly long _maxSliceBudgetTicks;

        private int _count = 0;
        private TimeSliceSlot[] _slots;

        private int _activeHead = InvalidIndex;
        private int _activeTail = InvalidIndex;

        private int _scheduleCursor = InvalidIndex;
        private int _scheduleStart = InvalidIndex;

        private int _freeHead = InvalidIndex;
        private int _pauseHead = InvalidIndex;
        
        /// <summary>
        /// 创建分帧离散调度器
        /// </summary>
        /// <param name="budgetUs">每次调度的总时间预算（微秒）</param>
        /// <param name="maxSliceBudgetUs">单个任务每次执行的最大时间片（微秒）</param>
        /// <param name="capacity">初始任务容量</param>
        /// <exception cref="ArgumentOutOfRangeException">时间预算配置无效</exception>
        public TimeSliceScheduler(int budgetUs, int maxSliceBudgetUs, int capacity = 64)
        {
            if (budgetUs <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(budgetUs), budgetUs, "总时间预算必须大于0");
            }
            if (maxSliceBudgetUs <= 0 || maxSliceBudgetUs > budgetUs)
            {
                throw new ArgumentOutOfRangeException(nameof(maxSliceBudgetUs), maxSliceBudgetUs, "单任务时间片必须大于0且不超过总时间预算");
            }

            _budgetUs = budgetUs;
            _budgetTicks = Math.Max(1L, budgetUs * Stopwatch.Frequency / 1_000_000L);
            _maxSliceBudgetTicks = Math.Max(1L, maxSliceBudgetUs * Stopwatch.Frequency / 1_000_000L);
            _slots = new TimeSliceSlot[capacity > 0 ? capacity : 4];
        }

        /// <summary>
        /// 清空队列
        /// </summary>
        public void Cleanup()
        {
            for (var index = 0; index < _count; ++index)
            {
                _slots[index].Invalidate();
                _slots[index].PrevIndex = _slots[index].NextIndex = InvalidIndex;
            }
            _count = 0;
            _freeHead = InvalidIndex;
            _pauseHead = InvalidIndex;
            _activeHead = InvalidIndex;
            _activeTail = InvalidIndex;
            _scheduleCursor = InvalidIndex;
            _scheduleStart = InvalidIndex;
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="callback">任务回调</param>
        /// <returns>任务Token</returns>
        public TimeSliceToken Add(TimeSliceCallback callback)
        {
            if (callback == null)
            {
                return default;
            }
            
            int index;
            if (_freeHead == InvalidIndex)
            {
                EnsureCapacity();
                index = _count++;
            }
            else
            {
                index = _freeHead;
                _freeHead = _slots[_freeHead].NextIndex;
            }

            ref var slot = ref _slots[index];
            slot.Paused = false;
            slot.Callback = callback;
            slot.EmaTicks = Stopwatch.Frequency / 10_000;

            if (_activeHead == InvalidIndex)
            {
                _activeHead = index;
                _activeTail = index;
                slot.PrevIndex = InvalidIndex;
                slot.NextIndex = InvalidIndex;
            }
            else
            {
                // 能跑进来_activeTail必定是有效的
                _slots[_activeTail].NextIndex = index;
                slot.PrevIndex = _activeTail;
                slot.NextIndex = InvalidIndex;
                _activeTail = index;
            }

            return new TimeSliceToken(this, index, slot.Version);
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="token">要暂停的任务Token</param>
        public void Pause(in TimeSliceToken token)
        {
            if (token.Scheduler != this || token.Index < 0 || token.Index >= _count)
            {
                return;
            }

            ref var slot = ref _slots[token.Index];
            if (slot.Paused || slot.Version != token.Version)
            {
                return;
            }

            RemoveFromActive(token.Index);

            slot.Paused = true;
            slot.PrevIndex = InvalidIndex;
            slot.NextIndex = _pauseHead;
            
            if (_pauseHead != InvalidIndex)
            {
                _slots[_pauseHead].PrevIndex = token.Index;
            }
            _pauseHead = token.Index;
        }

        /// <summary>
        /// 恢复任务
        /// </summary>
        /// <param name="token">要恢复任务Token</param>
        public void Resume(in TimeSliceToken token)
        {
            if (token.Scheduler != this || token.Index < 0 || token.Index >= _count)
            {
                return;
            }

            ref var slot = ref _slots[token.Index];
            if (!slot.Paused || slot.Version != token.Version)
            {
                return;
            }

            RemoveFromPause(token.Index);
            
            slot.Paused = false;
            slot.PrevIndex = _activeTail;
            slot.NextIndex = InvalidIndex;
            
            if (_activeTail != InvalidIndex)
            {
                _slots[_activeTail].NextIndex = token.Index;
            }
            else
            {
                _activeHead = token.Index;
            }
            
            _activeTail = token.Index;
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        /// <param name="token">任务Token</param>
        public void Cancel(TimeSliceToken token)
        {
            if (token.Scheduler != this || token.Index >= _count || token.Index < 0)
            {
                return;
            }

            if (_slots[token.Index].Version != token.Version)
            {
                return;
            }

            ref var slot = ref _slots[token.Index];
            if (slot.Paused)
            {
                RemoveFromPause(token.Index);
            }
            else
            {
                RemoveFromActive(token.Index);
            }

            slot.Invalidate();
            slot.PrevIndex = InvalidIndex;
            slot.NextIndex = _freeHead;
            _freeHead = token.Index;
        }

        /// <summary>
        /// 根据固定时间预算执行调度任务
        /// 必定执行一次任务，可能会超预算
        /// </summary>
        /// <returns>剩余未使用的时间预算（微秒），超支时返回负数</returns>
        public int Schedule()
        {
            //active列表头是空的话表示当前调度队列是空的，直接返回不做任何处理
            if (_activeHead == InvalidIndex)
            {
                return _budgetUs;
            }

            var budgetTicks = _budgetTicks;

            _scheduleCursor = _scheduleCursor == InvalidIndex ? _activeHead : _scheduleCursor; //绕回头部
            _scheduleStart = _scheduleCursor; //把起点记录下来，在后面遍历的时间检查是否绕了一圈

            //必定执行一次
            ExecuteOnce(ref budgetTicks); 
            _scheduleCursor = _scheduleCursor == InvalidIndex ? _activeHead : _scheduleCursor; //绕回头部
            
            while (_scheduleCursor != InvalidIndex && _scheduleCursor != _scheduleStart)
            {
                if (budgetTicks < _slots[_scheduleCursor].EmaTicks)
                {
                    break;
                }

                ExecuteOnce(ref budgetTicks);
                _scheduleCursor = _scheduleCursor == InvalidIndex ? _activeHead : _scheduleCursor; //绕回头部
            }

            _scheduleStart = InvalidIndex;

            return (int)(budgetTicks * 1_000_000L / Stopwatch.Frequency);
        }

        private void EnsureCapacity()
        {
            if (_count == _slots.Length)
            {
                Array.Resize(ref _slots, _slots.Length * 2);
            }
        }

        private void ExecuteOnce(ref long budgetTicks)
        {
            var index = _scheduleCursor;
            ref var slot = ref _slots[index];
            _scheduleCursor = slot.NextIndex;

            var startTime = Stopwatch.GetTimestamp();
            var callbackBudgetTicks = Math.Min(budgetTicks, _maxSliceBudgetTicks);
            var budget = new TimeSliceBudget(startTime + callbackBudgetTicks);
            var version = slot.Version;
            try
            {
                slot.Callback(in budget);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            var ticks = Stopwatch.GetTimestamp() - startTime;
            budgetTicks -= ticks;

            // 有可能在回调里触发扩容，这里直接使用索引而不是ref对象来检查
            ref var currentSlot = ref _slots[index];
            if (version == currentSlot.Version)
            {
                currentSlot.EmaTicks += (ticks - currentSlot.EmaTicks) >> 3;
            }
        }

        private void RemoveFromActive(int index)
        {
            ref var slot = ref _slots[index];
            var prevIndex = slot.PrevIndex;
            var nextIndex = slot.NextIndex;

            if (prevIndex != InvalidIndex)
            {
                _slots[prevIndex].NextIndex = nextIndex;
            }
            if (nextIndex != InvalidIndex)
            {
                _slots[nextIndex].PrevIndex = prevIndex;
            }
            if (_activeHead == index)
            {
                _activeHead = nextIndex;
            }
            if (_activeTail == index)
            {
                _activeTail = prevIndex;
            }
            if (_scheduleCursor == index)
            {
                _scheduleCursor = nextIndex;
            }
            if (_scheduleStart == index)
            {
                _scheduleStart = nextIndex;
            }
            
            slot.PrevIndex = InvalidIndex;
            slot.NextIndex = InvalidIndex;
        }

        private void RemoveFromPause(int index)
        {
            ref var slot = ref _slots[index];
            var prevIndex = slot.PrevIndex;
            var nextIndex = slot.NextIndex;

            if (prevIndex != InvalidIndex)
            {
                _slots[prevIndex].NextIndex = nextIndex;
            }
            if (nextIndex != InvalidIndex)
            {
                _slots[nextIndex].PrevIndex = prevIndex;
            }
            if (_pauseHead == index)
            {
                _pauseHead = nextIndex;
            }
            
            slot.PrevIndex = InvalidIndex;
            slot.NextIndex = InvalidIndex;
        }
    }
}
