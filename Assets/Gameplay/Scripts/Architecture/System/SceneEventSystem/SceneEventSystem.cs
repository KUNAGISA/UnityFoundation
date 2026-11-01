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
        private readonly HashSet<int> _scheduledEventIds = new();
        private readonly List<SceneEventState> _sortedEvents = new();
        private readonly HashSet<int> _dirtyEventIds = new();
        private readonly List<int> _batchEventIds = new();
        private SubscriptionToken _batchCompletedToken;
        private SubscriptionToken _sceneEventTableReloadedToken;
        private SubscriptionToken _procedurePauseChangedToken;
        private ConditionSystem _conditionSystem;
        private ActionSystem _actionSystem;
        private IProcedureContext _procedureContext;
        // 状态接收方和行为分发可同步重入；版本只保护这些调用后的遍历及分发回调。
        private int _sceneEventVersion;

        protected override void OnInitialize()
        {
            _conditionSystem = this.GetSystem<ConditionSystem>();
            _actionSystem = this.GetSystem<ActionSystem>();
        }

        protected override void OnShutdown()
        {
            Clear();
            _conditionSystem = null;
            _actionSystem = null;
        }

        protected override void OnProcedureAttach(IProcedureContext context)
        {
            _sceneEventTableReloadedToken = this.SubscribeEvent<TableReloadedEvent<SceneEventTable>>(OnSceneEventTableReloaded);
            _procedurePauseChangedToken = this.SubscribeEvent<ProcedurePauseChangedEvent>(OnProcedurePauseChanged);
            _procedureContext = context;
            _batchCompletedToken = this.SubscribeEvent<ConditionBatchCompletedEvent>(OnConditionBatchCompleted);
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            _sceneEventTableReloadedToken.Cancel();
            _sceneEventTableReloadedToken = default;
            _procedurePauseChangedToken.Cancel();
            _procedurePauseChangedToken = default;
            _batchCompletedToken.Cancel();
            _batchCompletedToken = default;
            Clear();
            _procedureContext = null;
        }

        private void OnProcedurePauseChanged(ProcedurePauseChangedEvent evt)
        {
            foreach (var state in _sortedEvents)
            {
                if (evt.IsPaused)
                {
                    state.Observation.Pause();
                }
                else
                {
                    state.Observation.Resume();
                }
            }
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

        public void Trigger(SceneEventPoint point)
        {
            if (point == null)
            {
                return;
            }
            if (!_events.TryGetValue(point.EventId, out var state))
            {
                Debug.LogError($"[SceneEvent] 场景事件 [{point.EventId}] 未注册");
                return;
            }
            if (state.Point.TriggerMode != SceneEventTriggerMode.EnterTrigger)
            {
                return;
            }

            SubmitEvent(state);
        }

        private void OnConditionBatchCompleted(ConditionBatchCompletedEvent evt)
        {
            var version = _sceneEventVersion;
            _batchEventIds.Clear();
            foreach (var eventId in _dirtyEventIds)
            {
                _batchEventIds.Add(eventId);
            }
            _dirtyEventIds.Clear();
            _batchEventIds.Sort();
            for (var index = 0; index < _batchEventIds.Count; ++index)
            {
                var state = _events[_batchEventIds[index]];
                var changed = ApplyTriggerableState(state);
                if (version != _sceneEventVersion)
                {
                    return;
                }
                if (changed && state.IsTriggerable && state.Point != null && state.Point.TriggerMode == SceneEventTriggerMode.Auto)
                {
                    SubmitEvent(state);
                    if (version != _sceneEventVersion)
                    {
                        return;
                    }
                }
            }
            _batchEventIds.Clear();
        }

        private void OnSceneEventTableReloaded(TableReloadedEvent<SceneEventTable> evt)
        {
            RebuildSceneEvents();
        }

        private void RebuildSceneEvents()
        {
            Clear();
            var version = _sceneEventVersion;
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
                state.OnDispatched = _ =>
                {
                    if (version == _sceneEventVersion)
                    {
                        OnActionDispatched(state);
                    }
                };
                _events.Add(state.EventId, state);
                _sortedEvents.Add(state);
            }

            _sortedEvents.Sort(static (left, right) => left.EventId.CompareTo(right.EventId));

            for (var index = 0; index < _sortedEvents.Count; ++index)
            {
                var state = _sortedEvents[index];
                state.Observation = _conditionSystem.Observe(state.Config.ConditionId, result =>
                {
                    state.ConditionResult = result;
                    _dirtyEventIds.Add(state.EventId);
                }, out var initialValue);
                state.ConditionResult = initialValue;
                if (_procedureContext.IsPaused)
                {
                    state.Observation.Pause();
                }
                ApplyTriggerableState(state, true);
                if (version != _sceneEventVersion)
                {
                    return;
                }
            }
            for (var index = 0; index < _sortedEvents.Count; ++index)
            {
                var state = _sortedEvents[index];
                if (state.IsTriggerable && state.Point != null && state.Point.TriggerMode == SceneEventTriggerMode.Auto)
                {
                    SubmitEvent(state);
                    if (version != _sceneEventVersion)
                    {
                        return;
                    }
                }
            }
        }

        private bool ApplyTriggerableState(SceneEventState state, bool initialize = false)
        {
            if (!initialize && state.IsTriggerable == state.ConditionResult)
            {
                return false;
            }
            state.IsTriggerable = state.ConditionResult;
            if (state.Point != null)
            {
                state.Point.NotifyStateChanged(state.IsTriggerable);
            }
            return true;
        }

        private void SubmitEvent(SceneEventState state)
        {
            if (state.Point == null || !_conditionSystem.Evaluate(state.Config.ConditionId) || !_scheduledEventIds.Add(state.EventId))
            {
                return;
            }

            _actionSystem.Submit(state.Config.ActionId, state.OnDispatched);
        }

        private void OnActionDispatched(SceneEventState state)
        {
            _scheduledEventIds.Remove(state.EventId);
        }

        private void Clear()
        {
            ++_sceneEventVersion;
            foreach (var state in _sortedEvents)
            {
                state.Observation.Cancel();
            }
            _dirtyEventIds.Clear();
            _batchEventIds.Clear();
            _events.Clear();
            _scheduledEventIds.Clear();
            _sortedEvents.Clear();
        }
    }
}
