using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class SceneNpcSystem : ProcedureCapacitySystem, ISystemAccessor, ISceneAfterEnter, ISceneBeforeLeave
    {
        private sealed class SceneNpcConfigState
        {
            public SceneNpcEntry Config;
            public ConditionObservationToken Observation;
            public bool IsConditionMet;
        }

        private readonly Dictionary<int, List<SceneNpcConfigState>> _configStatesByNpc = new();
        private readonly Dictionary<int, SceneNpcState> _activeNpcs = new();
        private readonly List<int> _npcIds = new();
        private readonly HashSet<int> _dirtyNpcIds = new();
        private readonly List<int> _batchNpcIds = new();
        
        private SubscriptionToken _sceneNpcTableReloadedToken;
        private SubscriptionToken _procedurePauseChangedToken;
        private ConditionSystem _conditionSystem;
        private SubscriptionToken _batchCompletedToken;
        private IProcedureContext _procedureContext;

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
            RebuildSceneNpcs();
            return UniTask.CompletedTask;
        }

        UniTask ISceneBeforeLeave.BeforeLeaveSceneAsync(CancellationToken cancellationToken)
        {
            foreach (var (_, state) in _activeNpcs)
            {
                if (state.Entity.IsValid)
                {
                    this.GetSystem<EntitySystem>().RemoveEntity(state.Entity.EntityId);
                }
            }

            Clear();
            return UniTask.CompletedTask;
        }

        protected override void OnProcedureAttach(IProcedureContext context)
        {
            _procedureContext = context;
            _sceneNpcTableReloadedToken = this.SubscribeEvent<TableReloadedEvent<SceneNpcTable>>(OnSceneNpcTableReloaded);
            _procedurePauseChangedToken = this.SubscribeEvent<ProcedurePauseChangedEvent>(OnProcedurePauseChanged);
            _batchCompletedToken = this.SubscribeEvent<ConditionBatchCompletedEvent>(OnConditionBatchCompleted);
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            Clear();
            _sceneNpcTableReloadedToken.Cancel();
            _sceneNpcTableReloadedToken = default;
            _procedurePauseChangedToken.Cancel();
            _procedurePauseChangedToken = default;
            _batchCompletedToken.Cancel();
            _batchCompletedToken = default;
            _procedureContext = null;
        }

        private void OnProcedurePauseChanged(ProcedurePauseChangedEvent evt)
        {
            foreach (var (_, states) in _configStatesByNpc)
            {
                foreach (var state in states)
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
        }

        private void OnConditionBatchCompleted(ConditionBatchCompletedEvent evt)
        {
            _batchNpcIds.Clear();
            foreach (var npcId in _dirtyNpcIds)
            {
                _batchNpcIds.Add(npcId);
            }
            _dirtyNpcIds.Clear();
            _batchNpcIds.Sort();
            for (var index = 0; index < _batchNpcIds.Count; ++index)
            {
                RefreshNpc(_batchNpcIds[index]);
            }
            _batchNpcIds.Clear();
        }

        private void OnSceneNpcTableReloaded(TableReloadedEvent<SceneNpcTable> evt)
        {
            RebuildSceneNpcs();
        }

        private void RebuildSceneNpcs()
        {
            ClearConfigStates();

            var sceneId = this.GetModel<GameStateModel>().CurrentSceneId;
            foreach (var entry in this.GetModel<TablesModel>().SceneNpcTable.DataList)
            {
                if (entry.SceneId != sceneId)
                {
                    continue;
                }
                
                if (!_configStatesByNpc.TryGetValue(entry.NpcId, out var states))
                {
                    states = new List<SceneNpcConfigState>();
                    _configStatesByNpc.Add(entry.NpcId, states);
                    _npcIds.Add(entry.NpcId);
                }
                states.Add(new SceneNpcConfigState { Config = entry });
            }

            foreach (var (_, states) in _configStatesByNpc)
            {
                states.Sort(static (left, right) => right.Config.Priority.CompareTo(left.Config.Priority));
            }

            _npcIds.Sort();
            foreach (var npcId in _npcIds)
            {
                foreach (var state in _configStatesByNpc[npcId])
                {
                    state.Observation = _conditionSystem.Observe(state.Config.ConditionId, result =>
                    {
                        state.IsConditionMet = result;
                        _dirtyNpcIds.Add(state.Config.NpcId);
                    }, out var initialValue);
                    state.IsConditionMet = initialValue;
                    if (_procedureContext.IsPaused)
                    {
                        state.Observation.Pause();
                    }
                }
            }
            RefreshAll();
        }

        private void ClearConfigStates()
        {
            foreach (var (_, states) in _configStatesByNpc)
            {
                foreach (var state in states)
                {
                    state.Observation.Cancel();
                }
            }
            _configStatesByNpc.Clear();
            _dirtyNpcIds.Clear();
            _batchNpcIds.Clear();
            _npcIds.Clear();
        }

        private void RefreshAll()
        {
            for (var index = 0; index < _npcIds.Count; ++index)
            {
                RefreshNpc(_npcIds[index]);
            }

            var activeIds = new List<int>(_activeNpcs.Keys);
            foreach (var npcId in activeIds)
            {
                if (!_configStatesByNpc.ContainsKey(npcId))
                {
                    RemoveNpc(npcId);
                }
            }
        }

        private void RefreshNpc(int npcId)
        {
            var selected = SelectConfig(_configStatesByNpc[npcId]);
            if (selected == null)
            {
                RemoveNpc(npcId);
                return;
            }
            
            if (_activeNpcs.TryGetValue(npcId, out var state) && state.Entity.IsValid)
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

        private SceneNpcEntry SelectConfig(List<SceneNpcConfigState> states)
        {
            foreach (var state in states)
            {
                if (state.IsConditionMet)
                {
                    return state.Config;
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

            if (state.Entity.IsValid)
            {
                this.GetSystem<EntitySystem>().RemoveEntity(state.Entity.EntityId);
            }
        }

        private void Clear()
        {
            ClearConfigStates();
            _activeNpcs.Clear();
        }
    }
}
