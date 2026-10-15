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
        private readonly Dictionary<int, List<SceneNpcEntry>> _configsByNpc = new();
        private readonly Dictionary<int, SceneNpcState> _activeNpcs = new();
        private readonly Dictionary<int, HashSet<int>> _npcIdsByCondition = new();
        private readonly Dictionary<int, ConditionWatchToken> _conditionWatches = new();
        
        private SubscriptionToken _conditionChangedToken;
        private SubscriptionToken _sceneNpcTableReloadedToken;
        private ConditionSystem _conditionSystem;
        private bool _isActive;
        private int _configVersion;

        protected override void OnInitialize()
        {
            _conditionSystem = this.GetSystem<ConditionSystem>();
            _conditionChangedToken = this.SubscribeEvent<ConditionResultChangedEvent>(OnConditionResultChanged);
            _sceneNpcTableReloadedToken = this.SubscribeEvent<TableReloadedEvent<SceneNpcTable>>(OnSceneNpcTableReloaded);
        }

        protected override void OnShutdown()
        {
            _sceneNpcTableReloadedToken.Cancel();
            _sceneNpcTableReloadedToken = default;

            _conditionChangedToken.Cancel();
            _conditionChangedToken = default;
            
            Clear();
            _conditionSystem = null;
            _isActive = false;
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
            _isActive = true;
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            _isActive = false;
            Clear();
        }

        private void OnConditionResultChanged(ConditionResultChangedEvent evt)
        {
            if (!_isActive || !_npcIdsByCondition.TryGetValue(evt.ConditionId, out var npcIds))
            {
                return;
            }

            var version = _configVersion;
            foreach (var npcId in npcIds)
            {
                if (version != _configVersion)
                {
                    return;
                }
                RefreshNpc(npcId);
            }
        }

        private void OnSceneNpcTableReloaded(TableReloadedEvent<SceneNpcTable> evt)
        {
            if (_isActive)
            {
                RebuildSceneNpc();
            }
        }

        private void RebuildSceneNpc()
        {
            _configsByNpc.Clear();
            ClearConditionWatches();

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
                }
                configs.Add(entry);
                if (!_npcIdsByCondition.TryGetValue(entry.ConditionId, out var npcIds))
                {
                    npcIds = new HashSet<int>();
                    _npcIdsByCondition.Add(entry.ConditionId, npcIds);
                }
                npcIds.Add(entry.NpcId);
            }

            foreach (var (_, configs) in _configsByNpc)
            {
                configs.Sort(static (left, right) => right.Priority.CompareTo(left.Priority));
            }

            foreach (var conditionId in _npcIdsByCondition.Keys)
            {
                _conditionWatches.Add(conditionId, _conditionSystem.WatchCondition(conditionId, out _));
            }

            RefreshAll();
        }

        private void ClearConditionWatches()
        {
            ++_configVersion;
            foreach (var token in _conditionWatches.Values)
            {
                token.Cancel();
            }
            _conditionWatches.Clear();
            _npcIdsByCondition.Clear();
        }

        private void RefreshAll()
        {
            foreach (var npcId in _configsByNpc.Keys)
            {
                RefreshNpc(npcId);
            }

            var activeIds = new List<int>(_activeNpcs.Keys);
            foreach (var npcId in activeIds)
            {
                if (!_configsByNpc.ContainsKey(npcId))
                {
                    RemoveNpc(npcId);
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
                ApplyConfigLocation(selected, state.Entity);
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
            ClearConditionWatches();
        }
    }
}
