using Aoiro;
using System;
using UnityEngine;

namespace Gameplay.Scheduler
{
    /// <summary>
    /// 帧调度器
    /// </summary>
    public sealed class FrameTickScheduler
    {
        private const int InvalidIndex = -1;

        private int _count = 0;
        private FrameTickSlot[] _slots = null;

        private int _scheduleCursor = InvalidIndex;
        private int _scheduleEnd = InvalidIndex;

        private int _activeHead = InvalidIndex;
        private int _activeTail = InvalidIndex;

        private int _freeHead = InvalidIndex;
        private int _pauseHead = InvalidIndex;

        public FrameTickScheduler(int capacity = 64)
        {
            _slots = new FrameTickSlot[capacity > 0 ? capacity : 4];
        }

        /// <summary>
        /// 清理帧调度器中的所有数据。
        /// 在调用此方法后，调度器将恢复到初始状态，任何之前添加的回调都将被移除。
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
            _scheduleEnd = InvalidIndex;
        }

        /// <summary>
        /// 向帧调度器中添加一个任务。
        /// </summary>
        /// <param name="callback">要在指定时机执行的回调函数。</param>
        /// <returns>任务Token</returns>
        public FrameTickToken Add(FrameTickAction callback)
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
            slot.Callback = callback;
            slot.Paused = false;
            
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

            return new FrameTickToken(this, index, slot.Version);
        }

        /// <summary>
        /// 暂停调度任务
        /// </summary>
        /// <param name="token">要暂停的任务Token</param>
        public void Pause(in FrameTickToken token)
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
        /// 恢复调度任务
        /// </summary>
        /// <param name="token">要恢复的任务Token</param>
        public void Resume(in FrameTickToken token)
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
        /// 取消帧调度
        /// </summary>
        /// <param name="token">需要取消的调度Token</param>
        public void Cancel(FrameTickToken token)
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
        /// 根据给定的帧时间执行所有已调度的回调。
        /// </summary>
        /// <param name="time">当前帧的时间信息，包括时间差和绝对时间等。</param>
        public void Schedule(in FrameTime time)
        {
            //active列表头是空的话表示当前调度队列是空的，直接返回不做任何处理
            if (_activeHead == InvalidIndex)
            {
                return;
            }

            _scheduleCursor = _activeHead;
            _scheduleEnd = _activeTail; //记录当前队列的尾部，防止遍历时添加的任务立刻执行
            
            while (_scheduleCursor != InvalidIndex)
            {
                var current = _scheduleCursor;
                ref readonly var slot = ref _slots[current];
                _scheduleCursor = slot.NextIndex;

                try
                {
                    slot.Callback?.Invoke(in time);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
                
                if (current == _scheduleEnd)
                {
                    break;
                }
            }

            _scheduleCursor = InvalidIndex;
            _scheduleEnd = InvalidIndex;
        }

        private void EnsureCapacity()
        {
            if (_count == _slots.Length)
            {
                Array.Resize(ref _slots, _slots.Length << 1);
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
            if (_scheduleEnd == index)
            {
                _scheduleEnd = prevIndex;
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
