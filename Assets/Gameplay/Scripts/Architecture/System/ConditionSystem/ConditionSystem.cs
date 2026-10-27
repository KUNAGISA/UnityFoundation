using System;
using System.Collections.Generic;
using Aoiro;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class ConditionSystem : SystemBase
    {
        private sealed class Observation
        {
            public readonly int ConditionId;
            public readonly HashSet<ConditionChangeType> Dependencies = new();
            public int WatchCount;
            public bool Result;

            public Observation(int conditionId, bool result)
            {
                ConditionId = conditionId;
                Result = result;
            }
        }

        private readonly Dictionary<int, Observation> _observations = new();
        private readonly Dictionary<long, Observation> _registrations = new();
        private readonly Dictionary<ConditionChangeType, HashSet<Observation>> _observationsByChangeType = new();
        private const int MaxRefreshCount = 1024;
        private readonly Queue<ConditionChangeType> _pendingRefreshes = new();
        private readonly List<Observation> _changedObservations = new();
        private readonly ConditionDependencyCollector _dependencyCollector = new();
        private ConditionContext _context;
        private TablesModel _tablesModel;
        private SubscriptionToken _gameIntVariableChangedToken;
        private SubscriptionToken _gameBoolVariableChangedToken;
        private SubscriptionToken _globalIntVariableChangedToken;
        private SubscriptionToken _globalBoolVariableChangedToken;
        private long _nextRegistrationId;
        private bool _isRefreshing;

        protected override void OnInitialize()
        {
            _context = BindContext(new ConditionContext());
            _tablesModel = this.GetModel<TablesModel>();
            _gameIntVariableChangedToken = this.SubscribeEvent<GameIntVariableChangedEvent>(_ => Refresh(ConditionChangeType.GameIntVariable));
            _gameBoolVariableChangedToken = this.SubscribeEvent<GameBoolVariableChangedEvent>(_ => Refresh(ConditionChangeType.GameBoolVariable));
            _globalIntVariableChangedToken = this.SubscribeEvent<GlobalIntVariableChangedEvent>(_ => Refresh(ConditionChangeType.GlobalIntVariable));
            _globalBoolVariableChangedToken = this.SubscribeEvent<GlobalBoolVariableChangedEvent>(_ => Refresh(ConditionChangeType.GlobalBoolVariable));
        }

        protected override void OnShutdown()
        {
            _gameIntVariableChangedToken.Cancel();
            _gameIntVariableChangedToken = default;
            _gameBoolVariableChangedToken.Cancel();
            _gameBoolVariableChangedToken = default;
            _globalIntVariableChangedToken.Cancel();
            _globalIntVariableChangedToken = default;
            _globalBoolVariableChangedToken.Cancel();
            _globalBoolVariableChangedToken = default;
            _registrations.Clear();
            _observations.Clear();
            _observationsByChangeType.Clear();
            _pendingRefreshes.Clear();
            _context = null;
            _tablesModel = null;
        }

        public ConditionWatchToken WatchCondition(int conditionId, out bool initialResult)
        {
            var registrationId = checked(++_nextRegistrationId);
            if (!_observations.TryGetValue(conditionId, out var observation))
            {
                observation = new Observation(conditionId, EvaluateCore(conditionId));
                _observations.Add(conditionId, observation);
                RegisterDependencies(observation);
            }

            _registrations.Add(registrationId, observation);
            ++observation.WatchCount;
            initialResult = observation.Result;
            return new ConditionWatchToken(this, registrationId);
        }

        public bool Evaluate(int conditionId, bool useCache = true)
        {
            if (useCache && _observations.TryGetValue(conditionId, out var observation))
            {
                return observation.Result;
            }
            return EvaluateCore(conditionId);
        }

        private bool EvaluateCore(int conditionId)
        {
            try
            {
                if (!_tablesModel.ConditionTable.TryGet(conditionId, out var entry))
                {
                    Debug.LogError($"[Condition] 条件配置不存在 [{conditionId}]");
                    return false;
                }
                return ConditionResolver.Evaluate(entry.Condition, _context);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        internal void CancelWatch(long registrationId)
        {
            if (!_registrations.Remove(registrationId, out var observation))
            {
                return;
            }
            if (--observation.WatchCount == 0)
            {
                _observations.Remove(observation.ConditionId);
                RemoveDependencies(observation);
            }
        }

        private void RegisterDependencies(Observation observation)
        {
            _dependencyCollector.Clear();
            if (_tablesModel.ConditionTable.TryGet(observation.ConditionId, out var entry))
            {
                ConditionDependencyResolver.CollectDependencies(entry.Condition, _dependencyCollector);
            }

            foreach (var type in _dependencyCollector.Types)
            {
                observation.Dependencies.Add(type);
                if (!_observationsByChangeType.TryGetValue(type, out var observations))
                {
                    observations = new HashSet<Observation>();
                    _observationsByChangeType.Add(type, observations);
                }
                observations.Add(observation);
            }
        }

        private void RemoveDependencies(Observation observation)
        {
            foreach (var type in observation.Dependencies)
            {
                var observations = _observationsByChangeType[type];
                observations.Remove(observation);
                if (observations.Count == 0)
                {
                    _observationsByChangeType.Remove(type);
                }
            }
            observation.Dependencies.Clear();
        }

        private void Refresh(ConditionChangeType type)
        {
            _pendingRefreshes.Enqueue(type);
            ProcessRefreshes();
        }

        public void RefreshAll()
        {
            // 外部入口在主线程调用，不支持从条件通知回调重入。
            _isRefreshing = true;
            try
            {
                _changedObservations.Clear();
                _observationsByChangeType.Clear();
                foreach (var observation in _observations.Values)
                {
                    observation.Dependencies.Clear();
                    RegisterDependencies(observation);
                    RefreshObservation(observation);
                }
                PublishChangedResults();
            }
            finally
            {
                _isRefreshing = false;
                ProcessRefreshes();
            }
        }

        private void ProcessRefreshes()
        {
            if (_isRefreshing)
            {
                return;
            }

            _isRefreshing = true;
            try
            {
                var refreshCount = 0;
                while (_pendingRefreshes.Count > 0)
                {
                    if (++refreshCount > MaxRefreshCount)
                    {
                        Debug.LogError($"[Condition] 单轮刷新超过 {MaxRefreshCount} 次，可能存在通知循环。");
                        break;
                    }

                    var type = _pendingRefreshes.Dequeue();
                    _changedObservations.Clear();
                    if (_observationsByChangeType.TryGetValue(type, out var observations))
                    {
                        foreach (var observation in observations)
                        {
                            RefreshObservation(observation);
                        }
                    }
                    PublishChangedResults();
                }
            }
            finally
            {
                _pendingRefreshes.Clear();
                _changedObservations.Clear();
                _isRefreshing = false;
            }
        }

        private void PublishChangedResults()
        {
            // 本轮先提交全部缓存并完成通知，再按顺序处理类型刷新。
            foreach (var observation in _changedObservations)
            {
                if (_observations.TryGetValue(observation.ConditionId, out var current) && ReferenceEquals(current, observation))
                {
                    this.PublishEvent(new ConditionResultChangedEvent(observation.ConditionId, observation.Result));
                }
            }
        }

        private void RefreshObservation(Observation observation)
        {
            var result = EvaluateCore(observation.ConditionId);
            if (observation.Result != result)
            {
                observation.Result = result;
                _changedObservations.Add(observation);
            }
        }
    }
}
