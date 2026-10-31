using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Scheduler;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class SceneNpcSystem : ProcedureCapacitySystem, ISystemAccessor, ISceneAfterEnter, ISceneBeforeLeave
    {
        private readonly Dictionary<int, List<SceneNpcEntry>> _configsByNpc = new();
        private readonly Dictionary<int, SceneNpcState> _activeNpcs = new();
        private readonly List<int> _npcIds = new();
        
        private TimeSliceToken _pollToken;
        private SubscriptionToken _sceneNpcTableReloadedToken;
        private SubscriptionToken _procedurePauseChangedToken;
        private ConditionSystem _conditionSystem;
        private bool _isPaused;
        private int _configVersion;
        private int _pollCursor;

        protected override void OnInitialize()
        {
            _conditionSystem = this.GetSystem<ConditionSystem>();
        }

        protected override void OnShutdown()
        {
            Clear();
            _conditionSystem = null;
        }

        UniTask ISceneAfterEnter.AfterEnterSceneAsync(SceneTransitionContext context, CancellationToken cancellationToken)
        {
            RebuildSceneNpc();
            return UniTask.CompletedTask;
        }

        UniTask ISceneBeforeLeave.BeforeLeaveSceneAsync(CancellationToken cancellationToken)
        {
            foreach (var (_, state) in _activeNpcs)
            {
                if (state.Entity != null && state.Entity.IsValid)
                {
                    this.GetSystem<EntitySystem>().RemoveEntity(state.Entity.EntityId);
                }
            }

            Clear();
            return UniTask.CompletedTask;
        }

        protected override void OnProcedureAttach(IProcedureContext context)
        {
            _sceneNpcTableReloadedToken = this.SubscribeEvent<TableReloadedEvent<SceneNpcTable>>(OnSceneNpcTableReloaded);
            _procedurePauseChangedToken = this.SubscribeEvent<ProcedurePauseChangedEvent>(OnProcedurePauseChanged);
            _isPaused = context.IsPaused;
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            _sceneNpcTableReloadedToken.Cancel();
            _sceneNpcTableReloadedToken = default;
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

        private void PollConditions(in TimeSliceBudget budget)
        {
            if (_npcIds.Count == 0)
            {
                return;
            }

            var version = _configVersion;
            var count = _npcIds.Count;
            for (var processed = 0; processed < count; ++processed)
            {
                var npcId = _npcIds[_pollCursor];
                _pollCursor = (_pollCursor + 1) % count;
                RefreshNpc(npcId);
                if (version != _configVersion || budget.ShouldYield)
                {
                    return;
                }
            }
        }

        private void OnSceneNpcTableReloaded(TableReloadedEvent<SceneNpcTable> evt)
        {
            RebuildSceneNpc();
        }

        private void RebuildSceneNpc()
        {
            _configsByNpc.Clear();
            CancelPolling();
            var version = _configVersion;

            var sceneId = this.GetModel<GameStateModel>().CurrentSceneId;
            foreach (var entry in this.GetModel<TablesModel>().SceneNpcTable.DataList)
            {
                if (entry.SceneId != sceneId)
                {
                    continue;
                }
                
                if (!_configsByNpc.TryGetValue(entry.NpcId, out var configs))
                {
                    configs = new List<SceneNpcEntry>();
                    _configsByNpc.Add(entry.NpcId, configs);
                    _npcIds.Add(entry.NpcId);
                }
                configs.Add(entry);
            }

            foreach (var (_, configs) in _configsByNpc)
            {
                configs.Sort(static (left, right) => right.Priority.CompareTo(left.Priority));
            }

            _npcIds.Sort();
            RefreshAll();
            if (version == _configVersion && _npcIds.Count > 0)
            {
                _pollToken = this.GetSystem<SchedulerSystem>().AddTimeSliceTask(PollConditions);
                if (_isPaused)
                {
                    _pollToken.Pause();
                }
            }
        }

        private void CancelPolling()
        {
            _configVersion = unchecked(_configVersion + 1);
            _pollToken.Cancel();
            _pollToken = default;
            _pollCursor = 0;
            _npcIds.Clear();
        }

        private void RefreshAll()
        {
            var version = _configVersion;
            for (var index = 0; index < _npcIds.Count; ++index)
            {
                RefreshNpc(_npcIds[index]);
                if (version != _configVersion)
                {
                    return;
                }
            }

            var activeIds = new List<int>(_activeNpcs.Keys);
            foreach (var npcId in activeIds)
            {
                if (!_configsByNpc.ContainsKey(npcId))
                {
                    RemoveNpc(npcId);
                    if (version != _configVersion)
                    {
                        return;
                    }
                }
            }
        }

        private void RefreshNpc(int npcId)
        {
            if (!_configsByNpc.TryGetValue(npcId, out var configs))
            {
                RemoveNpc(npcId);
                return;
            }

            var selected = SelectConfig(configs);
            if (selected == null)
            {
                RemoveNpc(npcId);
                return;
            }
            
            if (_activeNpcs.TryGetValue(npcId, out var state) && state.Entity != null && state.Entity.IsValid)
            {
                if (!ReferenceEquals(state.Config, selected))
                {
                    state.Config = selected;
                    ApplyConfigLocation(selected, state.Entity);
                }
                return;
            }

            RemoveNpc(npcId);
            SpawnNpc(selected);
        }

        private SceneNpcEntry SelectConfig(List<SceneNpcEntry> configs)
        {
            foreach (var config in configs)
            {
                if (_conditionSystem.Evaluate(config.ConditionId))
                {
                    return config;
                }
            }

            return null;
        }

        private void SpawnNpc(SceneNpcEntry config)
        {
            var entity = this.GetSystem<EntitySystem>().SpawnNpc(config.NpcId, config.Position, config.Rotation);
            if (entity == null)
            {
                return;
            }

            ApplyConfigLocation(config, entity);

            _activeNpcs[config.NpcId] = new SceneNpcState
            {
                NpcId = config.NpcId,
                Entity = entity,
                Config = config,
            };
        }

        private void ApplyConfigLocation(SceneNpcEntry config, NpcEntity entity)
        {
            if (config.RouteId.HasValue)
            {
                ApplyRouteLocation(config, entity);
                return;
            }

            entity.RequireAbility<CharacterMotorAbility>().WarpToWorld(config.Position, config.Rotation);
        }

        private void ApplyRouteLocation(SceneNpcEntry config, NpcEntity entity)
        {
            var routeModel = this.GetModel<MovementRouteModel>();
            if (!routeModel.TryProject(config.RouteId.Value, config.Position, out var location))
            {
                Debug.LogError($"[SceneNpc] 场景 NPC [{config.NpcId}] 无法投影到路线 [{config.RouteId.Value}]");
                return;
            }

            entity.RequireAbility<CharacterMotorAbility>().WarpToRoute(location, config.Rotation);
        }

        private void RemoveNpc(int npcId)
        {
            if (!_activeNpcs.Remove(npcId, out var state))
            {
                return;
            }

            if (state.Entity != null && state.Entity.IsValid)
            {
                this.GetSystem<EntitySystem>().RemoveEntity(state.Entity.EntityId);
            }
        }

        private void Clear()
        {
            _configsByNpc.Clear();
            _activeNpcs.Clear();
            CancelPolling();
        }
    }
}
