using System.Threading;
using System.Collections.Generic;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Scheduler;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class SceneEventSystem : ProcedureCapacitySystem, ISystemAccessor, ISceneAfterEnter, ISceneBeforeLeave
    {
        private readonly Dictionary<int, SceneEventState> _events = new();
        private readonly HashSet<int> _scheduledEventIds = new();
        private readonly List<SceneEventState> _sortedEvents = new();
        private TimeSliceToken _pollToken;
        private SubscriptionToken _sceneEventTableReloadedToken;
        private SubscriptionToken _procedurePauseChangedToken;
        private ConditionSystem _conditionSystem;
        private ActionSystem _actionSystem;
        private bool _isPaused;
        private int _registrationVersion;
        private int _pollCursor;

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
            _isPaused = context.IsPaused;
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            _sceneEventTableReloadedToken.Cancel();
            _sceneEventTableReloadedToken = default;
            _procedurePauseChangedToken.Cancel();
            _procedurePauseChangedToken = default;
            Clear();
        }

        private void OnProcedurePauseChanged(ProcedurePauseChangedEvent evt)
        {
            _isPaused = evt.IsPaused;
            if (_isPaused)
            {
                _pollToken.Pause();
            }
            else
            {
                _pollToken.Resume();
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

        private void PollConditions(in TimeSliceBudget budget)
        {
            if (_sortedEvents.Count == 0)
            {
                return;
            }

            var version = _registrationVersion;
            var count = _sortedEvents.Count;
            for (var processed = 0; processed < count; ++processed)
            {
                var state = _sortedEvents[_pollCursor];
                _pollCursor = (_pollCursor + 1) % count;
                var changed = RefreshTriggerableState(state);
                if (changed && state.IsTriggerable && IsCurrent(state, version) && state.Point.TriggerMode == SceneEventTriggerMode.Auto)
                {
                    SubmitEvent(state);
                }
                if (version != _registrationVersion || budget.ShouldYield)
                {
                    return;
                }
            }
        }

        private void OnSceneEventTableReloaded(TableReloadedEvent<SceneEventTable> evt)
        {
            RebuildSceneEvents();
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
                state.OnDispatched = _ => OnActionDispatched(state, version);
                _events.Add(state.EventId, state);
                _sortedEvents.Add(state);
            }

            _sortedEvents.Sort(static (left, right) => left.EventId.CompareTo(right.EventId));

            for (var index = 0; index < _sortedEvents.Count; ++index)
            {
                if (version != _registrationVersion)
                {
                    return;
                }
                RefreshTriggerableState(_sortedEvents[index], true);
            }
            for (var index = 0; index < _sortedEvents.Count; ++index)
            {
                if (version != _registrationVersion)
                {
                    return;
                }
                var state = _sortedEvents[index];
                if (state.IsTriggerable && state.Point.TriggerMode == SceneEventTriggerMode.Auto)
                {
                    SubmitEvent(state);
                }
            }
            if (version == _registrationVersion && _sortedEvents.Count > 0)
            {
                _pollToken = this.GetSystem<SchedulerSystem>().AddTimeSliceTask(PollConditions);
                if (_isPaused)
                {
                    _pollToken.Pause();
                }
            }
        }

        private bool RefreshTriggerableState(SceneEventState state, bool initialize = false)
        {
            var result = _conditionSystem.Evaluate(state.Config.ConditionId);
            if (!initialize && state.IsTriggerable == result)
            {
                return false;
            }

            state.IsTriggerable = result;
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

        private void OnActionDispatched(SceneEventState state, int version)
        {
            if (!IsCurrent(state, version))
            {
                return;
            }

            _scheduledEventIds.Remove(state.EventId);
        }

        private bool IsCurrent(SceneEventState state, int version)
        {
            return version == _registrationVersion && state.Point != null;
        }

        private void Clear()
        {
            _registrationVersion = unchecked(_registrationVersion + 1);
            _pollToken.Cancel();
            _pollToken = default;
            _pollCursor = 0;
            _events.Clear();
            _scheduledEventIds.Clear();
            _sortedEvents.Clear();
        }
    }
}
