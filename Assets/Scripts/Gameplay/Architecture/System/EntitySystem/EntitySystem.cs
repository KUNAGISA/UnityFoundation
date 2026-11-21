using System;
using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using Gameplay.Entities;
using Gameplay.Scheduler;
using Gameplay.Tables;
using UnityEngine;
using UObject = UnityEngine.Object;

namespace Gameplay
{
    public class EntitySystem : ProcedureCapacitySystem, IEntityReleaser, IScenePreload, ISceneBeforeEnter, ISceneBeforeLeave
    {
        internal bool HasActiveEntities => _world != null && _world.HasEntities;

        private readonly Dictionary<int, CharacterActor> _entitiesByCharacterId = new();
        private readonly Dictionary<string, IAssetAsyncOperation<GameObject>> _actorOperations = new(StringComparer.Ordinal);
        private readonly HashSet<string> _unavailableActorLocations = new(StringComparer.Ordinal);

        private IAssetService _assetService;
        private TablesModel _tablesModel;
        private MovementRouteModel _movementRoutes;

        private EntityWorld _world;
        private Transform _root;

        private FrameTickToken _frameTickToken;
        private FrameTickToken _lateTickToken;

        protected override void OnInitialize()
        {
            _assetService = this.GetService<IAssetService>();
            _tablesModel = this.GetModel<TablesModel>();
            _movementRoutes = this.GetModel<MovementRouteModel>();
            _world = new EntityWorld(this);
        }

        protected override void OnShutdown()
        {
            CleanupSceneState();
            _world = null;
            _tablesModel = null;
            _movementRoutes = null;
            _assetService = null;
        }

        protected override void OnProcedureAttach(IProcedureContext context)
        {
            _frameTickToken = context.AddFrameTickTask((in FrameTime time) => _world.FrameTick(time.DeltaTime));
            _lateTickToken = context.AddFrameTickTask((in FrameTime time) => _world.LateTick(time.DeltaTime), ProcedureTiming.LateTick);
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            _frameTickToken.Cancel();
            _lateTickToken.Cancel();
            _frameTickToken = default;
            _lateTickToken = default;
            CleanupSceneState();
        }

        public PlayerActor SpawnPlayer(in RouteLocation location, FacingDirection facing)
        {
            if (TryGetEntityByCharacterId(PlayerCharacterState.PlayerId, out var player))
            {
                return (PlayerActor)player;
            }

            var state = this.GetModel<CharacterModel>().PlayerCharacter ?? throw new InvalidOperationException("Player character state was not initialized.");
            state.Spatial.SetPosition(location.RouteId, location.Position);
            state.Spatial.SetFacing(facing);
            return SpawnActor<PlayerActor>(_tablesModel.PlayerCharacterTable.PrefabLocation, state, state.Spatial, location);
        }

        public NpcActor SpawnNpc(int npcId, Vector2 position, FacingDirection facing, RouteLocation? location)
        {
            if (!_tablesModel.NpcTable.TryGet(npcId, out var npcEntry))
            {
                Debug.LogError($"[Entities] Npc not found. NpcId: {npcId}");
                return null;
            }

            if (_unavailableActorLocations.Contains(npcEntry.PrefabLocation))
            {
                return null;
            }

            var state = this.GetModel<CharacterModel>().RequireNpc(npcId);
            var spatial = new SpatialState();
            spatial.SetPosition(location?.RouteId, location?.Position ?? position);
            spatial.SetFacing(facing);
            try
            {
                return SpawnActor<NpcActor>(npcEntry.PrefabLocation, state, spatial, location);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return null;
            }
        }

        public bool TryGetEntity(long entityId, out CharacterActor entity)
        {
            entity = null;
            if (_world == null || !_world.TryGetEntity(entityId, out var result) || result is not CharacterActor actor)
            {
                return false;
            }

            entity = actor;
            return true;
        }

        public bool TryGetEntityByCharacterId(int characterId, out CharacterActor entity)
        {
            entity = null;
            if (_world == null || !_entitiesByCharacterId.TryGetValue(characterId, out var result) || !result.IsValid)
            {
                return false;
            }

            entity = result;
            return true;
        }

        public void RemoveEntity(long entityId)
        {
            if (_world.TryGetEntity(entityId, out var result) && result is CharacterActor actor)
            {
                var characterId = actor.State.CharacterId;
                if (_entitiesByCharacterId.TryGetValue(characterId, out var indexedEntity) && ReferenceEquals(indexedEntity, actor))
                {
                    _entitiesByCharacterId.Remove(characterId);
                }
            }
            _world.RemoveEntity(entityId);
        }

        async UniTask IScenePreload.PreloadSceneAsync(SceneTransitionContext context, CancellationToken cancellationToken)
        {
            ReleaseActorOperations();
            try
            {
                var playerLocation = _tablesModel.PlayerCharacterTable.PrefabLocation;
                var locations = new HashSet<string>(StringComparer.Ordinal) { playerLocation };
                var tasks = new List<UniTask>
                {
                    PreloadActorAsync(playerLocation, true, cancellationToken)
                };

                foreach (var sceneNpc in _tablesModel.SceneNpcTable.DataList)
                {
                    if (sceneNpc.SceneId != context.Request.SceneId)
                    {
                        continue;
                    }

                    if (!_tablesModel.NpcTable.TryGet(sceneNpc.NpcId, out var npcEntry))
                    {
                        Debug.LogError($"[Entities] Npc not found while preloading scene. SceneId: {context.Request.SceneId}, NpcId: {sceneNpc.NpcId}");
                        continue;
                    }

                    if (locations.Add(npcEntry.PrefabLocation))
                    {
                        tasks.Add(PreloadActorAsync(npcEntry.PrefabLocation, false, cancellationToken));
                    }
                }

                await UniTask.WhenAll(tasks);
            }
            catch
            {
                ReleaseActorOperations();
                throw;
            }
        }

        UniTask ISceneBeforeEnter.BeforeEnterSceneAsync(SceneTransitionContext context, CancellationToken cancellationToken)
        {
            var root = new GameObject("[Entities]") { hideFlags = HideFlags.DontSave | HideFlags.NotEditable };
            _root = root.transform;
            return UniTask.CompletedTask;
        }

        UniTask ISceneBeforeLeave.BeforeLeaveSceneAsync(CancellationToken cancellationToken)
        {
            CleanupSceneState();
            return UniTask.CompletedTask;
        }

        void IEntityReleaser.Release(in EntityReleaseContext context)
        {
            if (context.Root != null)
            {
                UObject.Destroy(context.Root.gameObject);
            }
        }

        private async UniTask PreloadActorAsync(string location, bool required, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IAssetAsyncOperation<GameObject> operation = null;
            try
            {
                if (string.IsNullOrWhiteSpace(location))
                {
                    throw new InvalidOperationException("Actor prefab location is empty.");
                }

                operation = _assetService.LoadAssetAsync<GameObject>(location);
                _actorOperations.Add(location, operation);
                var prefab = await operation.ToUniTask(cancellationToken: cancellationToken, cancelImmediately: true);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Failed to load actor prefab at [{location}].");
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (operation != null && _actorOperations.TryGetValue(location, out var ownedOperation) && ReferenceEquals(ownedOperation, operation))
                {
                    _actorOperations.Remove(location);
                    operation.Release();
                }

                if (required)
                {
                    throw;
                }

                _unavailableActorLocations.Add(location);
                Debug.LogException(exception);
            }
        }

        private GameObject GetActorPrefab(string location)
        {
            if (location != null && _actorOperations.TryGetValue(location, out var cached))
            {
                return cached.Result;
            }

            IAssetAsyncOperation<GameObject> operation = null;
            try
            {
                if (string.IsNullOrWhiteSpace(location))
                {
                    throw new InvalidOperationException("Actor prefab location is empty.");
                }

                // Scene NPC configurations can be reloaded with new addresses after scene preload.
                operation = _assetService.LoadAssetSync<GameObject>(location);
                var prefab = operation.Result;
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Failed to load actor prefab at [{location}].");
                }

                _actorOperations.Add(location, operation);
                return prefab;
            }
            catch
            {
                _unavailableActorLocations.Add(location);
                operation?.Release();
                throw;
            }
        }

        private void CleanupSceneState()
        {
            _entitiesByCharacterId.Clear();
            try
            {
                _world.Cleanup();
            }
            finally
            {
                RemoveEntityRoot();
                ReleaseActorOperations();
            }
        }

        private void RemoveEntityRoot()
        {
            if (_root == null)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                UObject.DestroyImmediate(_root.gameObject);
            }
            else
            {
                UObject.Destroy(_root.gameObject);
            }
#else
            UObject.Destroy(_root.gameObject);
#endif
            _root = null;
        }

        private void ReleaseActorOperations()
        {
            foreach (var (_, operation) in _actorOperations)
            {
                operation.Release();
            }
            _actorOperations.Clear();
            _unavailableActorLocations.Clear();
        }

        private TActor SpawnActor<TActor>(string location, CharacterState state, SpatialState spatial, RouteLocation? routeLocation) where TActor : CharacterActor
        {
            if (_root == null)
            {
                throw new InvalidOperationException("EntitySystem is not ready.");
            }

            var characterId = state.CharacterId;
            if (_entitiesByCharacterId.ContainsKey(characterId))
            {
                throw new InvalidOperationException($"Character [{characterId}] has already been bound to an entity.");
            }

            var prefab = GetActorPrefab(location);
            if (!prefab.TryGetComponent<TActor>(out var prefabActor))
            {
                throw new InvalidOperationException($"Actor prefab [{location}] requires a {typeof(TActor).Name} on its root.");
            }

            var position = spatial.Position;
            var actor = UObject.Instantiate(prefabActor, new Vector3(position.x, position.y, 0f), Quaternion.identity, _root);
            actor.Prepare(state, spatial, _movementRoutes, routeLocation);
            _world.AddEntity(actor);

#if UNITY_EDITOR
            actor.name = $"{actor.GetType().Name} [EntityId: {actor.EntityId}, CharacterId: {characterId}]";
#endif

            _entitiesByCharacterId.Add(characterId, actor);
            return actor;
        }
    }
}
