using System.Threading;
using System.Collections.Generic;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class SceneEventSystem : ProcedureCapacitySystem, ISystemAccessor, ISceneAfterEnter, ISceneBeforeLeave
    {
        private readonly Dictionary<int, SceneEventState> _events = new();
        private readonly Dictionary<int, List<SceneEventState>> _eventsByCondition = new();
        private readonly Dictionary<int, ConditionWatchToken> _conditionWatches = new();
        private readonly HashSet<int> _scheduledEventIds = new();
        private readonly List<SceneEventState> _sortedEvents = new();
        private SubscriptionToken _conditionChangedToken;
        private SubscriptionToken _sceneEventTableReloadedToken;
        private ConditionSystem _conditionSystem;
        private ActionSystem _actionSystem;
        private bool _isActive;
        private int _registrationVersion;

        protected override void OnInitialize()
        {
            _conditionSystem = this.GetSystem<ConditionSystem>();
            _actionSystem = this.GetSystem<ActionSystem>();
            _conditionChangedToken = this.SubscribeEvent<ConditionResultChangedEvent>(OnConditionResultChanged);
            _sceneEventTableReloadedToken = this.SubscribeEvent<TableReloadedEvent<SceneEventTable>>(OnSceneEventTableReloaded);
        }

        protected override void OnShutdown()
        {
            _isActive = false;
            _sceneEventTableReloadedToken.Cancel();
            _sceneEventTableReloadedToken = default;
            _conditionChangedToken.Cancel();
            _conditionChangedToken = default;
            Clear();
            _conditionSystem = null;
            _actionSystem = null;
        }

        protected override void OnProcedureAttach(IProcedureContext context)
        {
            _isActive = true;
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            _isActive = false;
            Clear();
        }

        UniTask ISceneAfterEnter.AfterEnterSceneAsync(SceneTransitionContext context, CancellationToken cancellationToken)
        {
            RebuildSceneEvents();
            return UniTask.CompletedTask;
        }

        UniTask ISceneBeforeLeave.BeforeLeaveSceneAsync(CancellationToken cancellationToken)
        {
            Clear();
            return UniTask.CompletedTask;
        }

        public bool TryTrigger(SceneEventPoint point)
        {
            if (!_isActive || point == null)
            {
                return false;
            }
            if (!_events.TryGetValue(point.EventId, out var state))
            {
                Debug.LogError($"[SceneEvent] 场景事件 [{point.EventId}] 未注册");
                return false;
            }
            if (state.Point.TriggerMode != SceneEventTriggerMode.EnterTrigger)
            {
                return false;
            }

            var version = _registrationVersion;
            RefreshTriggerableState(state, useCache: false);
            return IsCurrent(state, version) && SubmitEvent(state);
        }

        private void OnConditionResultChanged(ConditionResultChangedEvent evt)
        {
            if (!_isActive || !_eventsByCondition.TryGetValue(evt.ConditionId, out var states))
            {
                return;
            }

            // 映射列表只在重建时替换，回调重入不会改写正在遍历的列表。
            var version = _registrationVersion;
            for (var index = 0; index < states.Count; ++index)
            {
                if (version != _registrationVersion)
                {
                    return;
                }
                var state = states[index];
                RefreshTriggerableState(state);
                if (IsCurrent(state, version) && state.Point.TriggerMode == SceneEventTriggerMode.Auto)
                {
                    SubmitEvent(state);
                }
            }
        }

        private void OnSceneEventTableReloaded(TableReloadedEvent<SceneEventTable> evt)
        {
            if (_isActive)
            {
                RebuildSceneEvents();
            }
        }

        private void RebuildSceneEvents()
        {
            Clear();
            var version = _registrationVersion;
            var sceneEventTable = this.GetModel<TablesModel>().SceneEventTable;
            foreach (var point in UnityEngine.Object.FindObjectsByType<SceneEventPoint>(FindObjectsSortMode.None))
            {
                if (!sceneEventTable.TryGet(point.EventId, out var config))
                {
                    Debug.LogError($"[SceneEvent] 场景事件配置不存在 [{point.EventId}]");
                    continue;
                }
                if (_events.ContainsKey(point.EventId))
                {
                    Debug.LogError($"[SceneEvent] 场景中存在重复事件 Id [{point.EventId}]");
                    continue;
                }

                var state = new SceneEventState { EventId = point.EventId, Point = point, Config = config };
                state.OnExecuted = success => CompleteEvent(state, version, success);
                _events.Add(state.EventId, state);
                _sortedEvents.Add(state);
                if (!_eventsByCondition.TryGetValue(config.ConditionId, out var states))
                {
                    states = new List<SceneEventState>();
                    _eventsByCondition.Add(config.ConditionId, states);
                }
                states.Add(state);
            }

            foreach (var states in _eventsByCondition.Values)
            {
                states.Sort(static (left, right) => left.EventId.CompareTo(right.EventId));
            }
            _sortedEvents.Sort(static (left, right) => left.EventId.CompareTo(right.EventId));
            foreach (var conditionId in _eventsByCondition.Keys)
            {
                _conditionWatches.Add(conditionId, _conditionSystem.WatchCondition(conditionId, out _));
            }

            for (var index = 0; index < _sortedEvents.Count; ++index)
            {
                if (version != _registrationVersion)
                {
                    return;
                }
                RefreshTriggerableState(_sortedEvents[index]);
            }
            for (var index = 0; index < _sortedEvents.Count; ++index)
            {
                if (version != _registrationVersion)
                {
                    return;
                }
                var state = _sortedEvents[index];
                if (state.Point.TriggerMode == SceneEventTriggerMode.Auto)
                {
                    SubmitEvent(state);
                }
            }
        }

        private void RefreshTriggerableState(SceneEventState state, bool useCache = true)
        {
            state.IsTriggerable = _conditionSystem.Evaluate(state.Config.ConditionId, useCache);
            if (state.Point != null)
            {
                state.Point.NotifyStateChanged(state.IsTriggerable);
            }
        }

        private bool SubmitEvent(SceneEventState state)
        {
            if (!_isActive || state.Point == null || !state.IsTriggerable || !_scheduledEventIds.Add(state.EventId))
            {
                return false;
            }

            _actionSystem.Submit(state.Config.ActionId, state.OnExecuted);
            return true;
        }

        private void CompleteEvent(SceneEventState state, int version, bool success)
        {
            if (!IsCurrent(state, version))
            {
                return;
            }

            _scheduledEventIds.Remove(state.EventId);
            if (!success)
            {
                Debug.LogError($"[SceneEvent] 场景事件 [{state.EventId}] 行为发起失败。");
            }
            RefreshTriggerableState(state);
        }

        private bool IsCurrent(SceneEventState state, int version)
        {
            return _isActive && version == _registrationVersion && state.Point != null;
        }

        private void Clear()
        {
            _registrationVersion = unchecked(_registrationVersion + 1);
            foreach (var token in _conditionWatches.Values)
            {
                token.Cancel();
            }
            _conditionWatches.Clear();
            _events.Clear();
            _eventsByCondition.Clear();
            _scheduledEventIds.Clear();
            _sortedEvents.Clear();
        }
    }
}
