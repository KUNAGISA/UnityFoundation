using System;
using System.Collections.Generic;
using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class ConditionSystem : SystemBase
    {
        private sealed class ObservedCondition
        {
            public int Id;
            public ConditionSource Sources;
            public bool Value;
            public bool NeedsEvaluation;
            public bool NeedsSourceRefresh;
            public readonly List<Observation> Observers = new();
        }

        private sealed class Observation
        {
            public long Id;
            public ObservedCondition Condition;
            public Action<bool> Callback;
            public bool IsPaused;
            public bool LastNotifiedValue;
        }

        private readonly struct Notification
        {
            public readonly Observation Observer;
            public readonly bool Value;

            public Notification(Observation observer, bool value)
            {
                Observer = observer;
                Value = value;
            }
        }

        private readonly Dictionary<int, ObservedCondition> _conditions = new();
        private readonly Dictionary<long, Observation> _observations = new();
        private readonly List<ObservedCondition> _evaluationBatch = new();
        private readonly List<Notification> _notifications = new();
        private readonly List<SubscriptionToken> _subscriptions = new();
        private ConditionContext _context;
        private TablesModel _tablesModel;
        private ConditionSource _dirtySources;
        private long _nextObservationId;
        private bool _isFlushing;
        private bool _hasPending;

        protected override void OnInitialize()
        {
            _context = BindContext(new ConditionContext());
            _tablesModel = this.GetModel<TablesModel>();
            _subscriptions.Add(this.SubscribeEvent<GameVariableChangedEvent>(_ => MarkDirty(ConditionSource.GameVariables)));
            _subscriptions.Add(this.SubscribeEvent<GameSwitchChangedEvent>(_ => MarkDirty(ConditionSource.GameSwitches)));
            _subscriptions.Add(this.SubscribeEvent<GlobalVariableChangedEvent>(_ => MarkDirty(ConditionSource.GlobalVariables)));
            _subscriptions.Add(this.SubscribeEvent<GlobalSwitchChangedEvent>(_ => MarkDirty(ConditionSource.GlobalSwitches)));
            _subscriptions.Add(this.SubscribeEvent<TableReloadedEvent<GameVariableTable>>(_ => MarkDirty(ConditionSource.GameVariables)));
            _subscriptions.Add(this.SubscribeEvent<TableReloadedEvent<GameSwitchTable>>(_ => MarkDirty(ConditionSource.GameSwitches)));
            _subscriptions.Add(this.SubscribeEvent<TableReloadedEvent<GlobalVariableTable>>(_ => MarkDirty(ConditionSource.GlobalVariables)));
            _subscriptions.Add(this.SubscribeEvent<TableReloadedEvent<GlobalSwitchTable>>(_ => MarkDirty(ConditionSource.GlobalSwitches)));
            _subscriptions.Add(this.SubscribeEvent<TableReloadedEvent<ConditionTable>>(_ => RebuildDependencies()));
        }

        protected override void OnShutdown()
        {
            foreach (var subscription in _subscriptions)
            {
                subscription.Cancel();
            }
            _subscriptions.Clear();
            _observations.Clear();
            _conditions.Clear();
            _dirtySources = ConditionSource.None;
            _hasPending = false;
            _context = null;
            _tablesModel = null;
        }

        public bool Evaluate(int conditionId)
        {
            if (!_tablesModel.ConditionTable.TryGet(conditionId, out var entry))
            {
                Debug.LogError($"[Condition] 条件配置不存在 [{conditionId}]");
                return false;
            }
            return ConditionResolver.Evaluate(entry.Condition, _context);
        }

        public ConditionObservationToken Observe(int conditionId, Action<bool> onChanged, out bool initialValue)
        {
            if (onChanged == null)
            {
                throw new ArgumentNullException(nameof(onChanged));
            }
            initialValue = Evaluate(conditionId);
            if (!_conditions.TryGetValue(conditionId, out var condition))
            {
                condition = new ObservedCondition { Id = conditionId, Value = initialValue, Sources = CollectSources(conditionId) };
                _conditions.Add(conditionId, condition);
            }
            else
            {
                // 注册读取当前值，但不吞掉既有观察者尚未收到的变化。
                condition.NeedsEvaluation = true;
                _hasPending = true;
            }
            var observation = new Observation { Id = ++_nextObservationId, Condition = condition, Callback = onChanged, LastNotifiedValue = initialValue };
            condition.Observers.Add(observation);
            _observations.Add(observation.Id, observation);
            return new ConditionObservationToken(this, observation.Id);
        }

        public void CancelObservation(long observationId)
        {
            if (!_observations.Remove(observationId, out var observation))
            {
                return;
            }
            var condition = observation.Condition;
            condition.Observers.Remove(observation);
            if (condition.Observers.Count == 0)
            {
                _conditions.Remove(condition.Id);
            }
        }

        public void PauseObservation(long observationId)
        {
            if (_observations.TryGetValue(observationId, out var observation))
            {
                observation.IsPaused = true;
            }
        }

        public void ResumeObservation(long observationId)
        {
            if (_observations.TryGetValue(observationId, out var observation) && observation.IsPaused)
            {
                observation.IsPaused = false;
                observation.Condition.NeedsEvaluation = true;
                _hasPending = true;
            }
        }

        public void FlushDirty()
        {
            if (_isFlushing || !_hasPending)
            {
                return;
            }
            _isFlushing = true;
            try
            {
                _hasPending = false;
                var sources = _dirtySources;
                _dirtySources = ConditionSource.None;
                CollectDirtyConditions(sources);
                EvaluateBatch();
                DispatchNotifications();
                if (_evaluationBatch.Count != 0)
                {
                    this.PublishEvent(new ConditionBatchCompletedEvent());
                }
            }
            finally
            {
                _notifications.Clear();
                _evaluationBatch.Clear();
                _isFlushing = false;
            }
        }

        private void CollectDirtyConditions(ConditionSource sources)
        {
            foreach (var pair in _conditions)
            {
                var condition = pair.Value;
                if (condition.NeedsEvaluation || condition.NeedsSourceRefresh || (condition.Sources & sources) != 0)
                {
                    _evaluationBatch.Add(condition);
                }
            }
            _evaluationBatch.Sort(static (left, right) => left.Id.CompareTo(right.Id));
        }

        private void EvaluateBatch()
        {
            foreach (var condition in _evaluationBatch)
            {
                condition.NeedsEvaluation = false;
                if (condition.NeedsSourceRefresh)
                {
                    condition.NeedsSourceRefresh = false;
                    condition.Sources = CollectSources(condition.Id);
                }
                condition.Value = Evaluate(condition.Id);
                foreach (var observer in condition.Observers)
                {
                    if (!observer.IsPaused && observer.LastNotifiedValue != condition.Value)
                    {
                        _notifications.Add(new Notification(observer, condition.Value));
                    }
                }
            }
        }

        private void DispatchNotifications()
        {
            foreach (var notification in _notifications)
            {
                var observer = notification.Observer;
                if (!_observations.ContainsKey(observer.Id) || observer.IsPaused)
                {
                    continue;
                }
                observer.LastNotifiedValue = notification.Value;
                try
                {
                    observer.Callback(notification.Value);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }
            }
        }

        private void MarkDirty(ConditionSource sources)
        {
            _dirtySources |= sources;
            _hasPending = true;
        }

        private void RebuildDependencies()
        {
            _hasPending = true;
            foreach (var pair in _conditions)
            {
                pair.Value.NeedsSourceRefresh = true;
            }
        }

        private ConditionSource CollectSources(int conditionId)
        {
            if (!_tablesModel.ConditionTable.TryGet(conditionId, out var entry))
            {
                return ConditionSource.None;
            }
            return ConditionResolver.CollectSources(entry.Condition);
        }
    }
}
