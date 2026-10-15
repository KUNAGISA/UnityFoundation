using System;
using Aoiro;
using UnityEngine;

namespace Gameplay.Scheduler
{
    /// <summary>
    /// 定时器调度器
    /// </summary>
    public sealed class TimerScheduler
    {
        /// <summary>
        /// 时间精度，毫秒
        /// </summary>
        private const uint TimeAccuracy = 1u;

        private const int TvrBits = 8;
        private const int TvnBits = 6;
        private const int TvrSize = 1 << TvrBits;
        private const int TvnSize = 1 << TvnBits;
        private const int TvrMask = TvrSize - 1;
        private const int TvnMask = TvnSize - 1;

        private const int Tv1Offset = 0;
        private const int Tv2Offset = TvrSize;
        private const int Tv3Offset = TvrSize + TvnSize;

        private const int InvalidIndex = -1;
        private const int InfiniteTimes = -1;

        private ulong _elapsed = 0L;
        private ulong _jiffies = 0L;

        private int _freeHead = InvalidIndex;
        private readonly int[] _buckets = new int[TvrSize + TvnSize + TvnSize];

        private int _count = 0;
        private TimerSlot[] _slots = null;

        public TimerScheduler(int capacity = 64)
        {
            _slots = new TimerSlot[capacity > 0 ? capacity : 4];
            Array.Fill(_buckets, InvalidIndex);
        }
        
        /// <summary>
        /// 清空数据
        /// </summary>
        public void Cleanup()
        {
            for (var index = 0; index < _count; ++index)
            {
                ref var slot = ref _slots[index];
                slot.Invalidate();
                slot.PrevIndex = slot.NextIndex = slot.BucketIndex = InvalidIndex;
            }
            Array.Fill(_buckets, InvalidIndex);

            _count = 0;
            _jiffies = 0UL;
            _elapsed = 0L;
            _freeHead = InvalidIndex;
        }

        /// <summary>
        /// 添加定时任务
        /// </summary>
        /// <param name="intervalMs">定时器触发的时间间隔，单位为毫秒</param>
        /// <param name="callback">定时器触发时执行的回调函数</param>
        /// <param name="times">定时器触发的次数，默认为1次。如果设置为负数，则表示无限次触发</param>
        /// <param name="delayMs">定时器首次触发前的延迟时间，单位为毫秒。如果不指定，则默认等于intervalMs</param>
        /// <returns>定时器任务Token</returns>
        public TimerToken Add(ulong intervalMs, Action callback, int times = 1, ulong? delayMs = null)
        {
            if (times == 0)
            {
                return default;
            }

            times = times > 0 ? times : InfiniteTimes;
            var interval = Math.Max(1UL, intervalMs / TimeAccuracy); //确保至少有1间隔;
            var delay = delayMs.HasValue ? Math.Max(1UL, delayMs.Value / TimeAccuracy) : interval;

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
            slot.Expires = _jiffies + delay;
            slot.Times = times;
            slot.Callback = callback;
            slot.Interval = interval;
            slot.Paused = false;
            slot.Remaining = 0UL;
            
            ReBucketSlot(index, ref slot);
            return new TimerToken(this, index, slot.Version);
        }

        /// <summary>
        /// 暂停定时器
        /// </summary>
        /// <param name="token">定时器任务Token</param>
        public void Pause(in TimerToken token)
        {
            if (token.Scheduler != this || token.Index >= _count || token.Index < 0)
            {
                return;
            }

            ref var slot = ref _slots[token.Index];
            if (slot.Version != token.Version || slot.Paused)
            {
                return;
            }

            DetachFromBucket(token.Index, ref slot);

            slot.Paused = true;
            slot.Remaining = slot.Expires > _jiffies ? (slot.Expires - _jiffies) : 0UL;
        }

        /// <summary>
        /// 恢复定时器
        /// </summary>
        /// <param name="token">定时器任务Token</param>
        public void Resume(in TimerToken token)
        {
            if (token.Scheduler != this || token.Index >= _count || token.Index < 0)
            {
                return;
            }

            ref var slot = ref _slots[token.Index];
            if (slot.Version != token.Version || !slot.Paused)
            {
                return;
            }

            slot.Paused = false;
            slot.Expires = _jiffies + slot.Remaining;
            ReBucketSlot(token.Index, ref slot);
        }

        /// <summary>
        /// 取消定时器
        /// </summary>
        /// <param name="token">定时器任务Token</param>
        public void Cancel(TimerToken token)
        {
            if (token.Scheduler != this || token.Index >= _count || token.Index < 0)
            {
                return;
            }

            ref var slot = ref _slots[token.Index];
            if (slot.Version != token.Version)
            {
                return;
            }

            if (slot.BucketIndex != InvalidIndex)
            {
                DetachFromBucket(token.Index, ref slot);
            }
            FreeSlot(token.Index, ref slot);
        }

        /// <summary>
        /// 根据给定的时间增量更新定时器调度
        /// </summary>
        /// <param name="deltaTimeMs">自上次调用以来经过的时间，单位为毫秒</param>
        public void Schedule(ulong deltaTimeMs)
        {
            _elapsed += deltaTimeMs;
            while (_elapsed >= TimeAccuracy)
            {
                _elapsed -= TimeAccuracy;
                Schedule();
            }
        }

        private void EnsureCapacity()
        {
            if (_count == _slots.Length)
            {
                Array.Resize(ref _slots, _slots.Length << 1);
            }
        }

        private void Schedule()
        {
            var tv1Slot = (int)(_jiffies & TvrMask);
            if (tv1Slot == 0)
            {
                var tv2Slot = (int)((_jiffies >> TvrBits) & TvnMask);
                Cascade(Tv2Offset + tv2Slot);

                if (tv2Slot == 0)
                {
                    var tv3Slot = (int)((_jiffies >> (TvrBits + TvnBits)) & TvnMask);
                    Cascade(Tv3Offset + tv3Slot);
                }
            }

            Process(Tv1Offset + tv1Slot);
            ++_jiffies;
        }

        private void Process(int bucketIndex)
        {
            for (var index = _buckets[bucketIndex]; index != InvalidIndex; index = _buckets[bucketIndex])
            {
                ref var slot = ref _slots[index];
                var version = slot.Version;
                DetachFromBucket(index, ref slot); //先从bucket里移出

                try
                {
                    slot.Callback();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                ref var currentSlot = ref _slots[index];
                if (version != currentSlot.Version)
                {
                    continue;
                }

                if (currentSlot.Times != InfiniteTimes)
                {
                    --currentSlot.Times;
                }

                if (currentSlot.Times != 0)
                {
                    currentSlot.Expires = _jiffies + currentSlot.Interval;
                    ReBucketSlot(index, ref currentSlot);
                }
                else
                {
                    FreeSlot(index, ref currentSlot);
                }
            }
        }

        private void Cascade(int bucketIndex)
        {
            var slotIndex = _buckets[bucketIndex];
            _buckets[bucketIndex] = InvalidIndex; //断开当前 bucket 与链表的关联，防止级联时新 re-bucket 的节点被再次处理

            while (slotIndex != InvalidIndex)
            {
                ref var slot = ref _slots[slotIndex];
                var nextIndex = slot.NextIndex;

                //简单重置数据
                slot.BucketIndex = InvalidIndex;
                slot.NextIndex = slot.PrevIndex = InvalidIndex;

                ReBucketSlot(slotIndex, ref slot);
                slotIndex = nextIndex;
            }
        }

        private void FreeSlot(int nodeIndex, ref TimerSlot slot)
        {
            slot.Invalidate();
            slot.BucketIndex = InvalidIndex;
            slot.PrevIndex = InvalidIndex;
            slot.NextIndex = _freeHead;
            _freeHead = nodeIndex;
        }
        
        private void DetachFromBucket(int slotIndex, ref TimerSlot slot)
        {
            var bucketHeadIndex = _buckets[slot.BucketIndex];
            if (slotIndex == bucketHeadIndex)
            {
                _buckets[slot.BucketIndex] = slot.NextIndex;
            }

            if (slot.NextIndex != InvalidIndex)
            {
                _slots[slot.NextIndex].PrevIndex = slot.PrevIndex;
            }
            if (slot.PrevIndex != InvalidIndex)
            {
                _slots[slot.PrevIndex].NextIndex = slot.NextIndex;
            }

            slot.BucketIndex = InvalidIndex;
            slot.NextIndex = slot.PrevIndex = InvalidIndex;
        }

        private void ReBucketSlot(int slotIndex, ref TimerSlot slot)
        {
            var delta = slot.Expires > _jiffies ? (slot.Expires - _jiffies) : 0UL;
            var bucketIndex = (delta) switch
            {
                < TvrSize => Tv1Offset + (int)(slot.Expires & TvrMask),
                < 1UL << (TvrBits + TvnBits) => Tv2Offset + (int)((slot.Expires >> TvrBits) & TvnMask),
                _ => Tv3Offset + (int)((slot.Expires >> (TvrBits + TvnBits)) & TvnMask)
            };

            var bucketHeadIndex = _buckets[bucketIndex];
            if (bucketHeadIndex != InvalidIndex)
            {
                _slots[bucketHeadIndex].PrevIndex = slotIndex;
            }

            slot.PrevIndex = InvalidIndex;
            slot.NextIndex = _buckets[bucketIndex];
            slot.BucketIndex = bucketIndex;
            _buckets[bucketIndex] = slotIndex;
        }
    }
}
