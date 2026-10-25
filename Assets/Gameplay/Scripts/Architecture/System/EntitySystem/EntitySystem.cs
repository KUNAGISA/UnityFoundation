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

        private readonly EntityContext _worldContext = new EntityContext();
        private readonly Dictionary<int, CharacterEntity> _entitiesByCharacterId = new();
        private readonly Dictionary<int, IAssetAsyncOperation<GameObject>> _actorOperations = new();

        private IAssetService _assetService;
        private TablesModel _tablesModel;

        private EntityWorld _world;
        private Transform _root;

        private FrameTickToken _frameTickToken;
        private FrameTickToken _lateTickToken;

        protected override void OnInitialize()
        {
            BindContext(_worldContext);
            _assetService = this.GetService<IAssetService>();
            _tablesModel = this.GetModel<TablesModel>();
            _world = new EntityWorld(_worldContext, this);
        }

        protected override void OnShutdown()
        {
            CleanupSceneState();
            _world = null;
            _tablesModel = null;
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
            ReleaseActorOperations();
        }

        public PlayerEntity SpawnPlayer(in RouteLocation location, Quaternion rotation)
        {
            if (TryGetEntityByCharacterId(PlayerCharacterState.PlayerId, out var playerCharacter))
            {
                return (PlayerEntity)playerCharacter;
            }

            var state = this.GetModel<CharacterModel>().PlayerCharacter ?? throw new InvalidOperationException("Player character state was not initialized.");
            var spatial = state.Spatial;
            spatial.SetPose(location.RouteId, location.Position, rotation);

            var player = new PlayerEntity(state);
            var actorId = _tablesModel.PlayerCharacterTable.ActorId;
            if (SpawnCharacter(player, actorId))
            {
                player.RequireAbility<CharacterMotorAbility>().WarpToRoute(location, rotation);
                return player;
            }
            return null;
        }

        public NpcEntity SpawnNpc(int npcId, Vector2 position, Quaternion rotation)
        {
            var state = this.GetModel<CharacterModel>().RequireNpc(npcId);

            if (!_tablesModel.NpcTable.TryGet(npcId, out var npcEntry))
            {
                Debug.LogError($"[Entities] Npc not found. NpcId: {npcId}");
                return null;
            }

            var spatial = new SpatialState();
            spatial.SetPose(null, position, rotation);
            var entity = new NpcEntity(state, spatial);
            return SpawnCharacter(entity, npcEntry.ActorId) ? entity : null;
        }

        public bool TryGetEntity(long entityId, out CharacterEntity entity)
        {
            entity = null;
            if (_world == null || !_world.TryGetEntity(entityId, out var result) || result is not CharacterEntity characterEntity)
            {
                return false;
            }

            entity = characterEntity;
            return true;
        }

        public bool TryGetEntityByCharacterId(int characterId, out CharacterEntity entity)
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
            if (_world.TryGetEntity(entityId, out var result) && result is CharacterEntity characterEntity)
            {
                var characterId = characterEntity.State.CharacterId;
                if (_entitiesByCharacterId.TryGetValue(characterId, out var indexedEntity) && ReferenceEquals(indexedEntity, characterEntity))
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
                var playerActorId = _tablesModel.PlayerCharacterTable.ActorId;
                var actorIds = new HashSet<int> { playerActorId };
                var tasks = new List<UniTask>
                {
                    PreloadActorAsync(playerActorId, true, cancellationToken)
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

                    if (actorIds.Add(npcEntry.ActorId))
                    {
                        tasks.Add(PreloadActorAsync(npcEntry.ActorId, false, cancellationToken));
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
            if (!context.Root)
            {
                return;
            }

            if (context.Root.TryGetComponent<CharacterActor>(out var actor))
            {
                actor.Unbind();
            }

            UObject.Destroy(context.Root.gameObject);
        }

        private async UniTask PreloadActorAsync(int actorId, bool required, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IAssetAsyncOperation<GameObject> operation = null;
            try
            {
                var location = GetActorLocation(actorId);
                operation = _assetService.LoadAssetAsync<GameObject>(location);
                _actorOperations.Add(actorId, operation);
                var prefab = await operation.ToUniTask(cancellationToken: cancellationToken, cancelImmediately: true);
                if (prefab == null)
                {
                    throw new InvalidOperationException($"Failed to load actor prefab. ActorId: {actorId}, Location: {location}");
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                if (operation != null)
                {
                    _actorOperations.Remove(actorId);
                    operation.Release();
                }
                if (required)
                {
                    throw;
                }
                Debug.LogException(exception);
            }
        }

        private string GetActorLocation(int actorId)
        {
            if (!_tablesModel.ActorTable.TryGet(actorId, out var actorEntry))
            {
                throw new InvalidOperationException($"Actor not found. ActorId: {actorId}");
            }

            if (string.IsNullOrEmpty(actorEntry.Location))
            {
                throw new InvalidOperationException($"Actor location is empty. ActorId: {actorId}");
            }

            return actorEntry.Location;
        }

        private bool TryGetOrLoadActorPrefab(int actorId, out GameObject prefab)
        {
            if (_actorOperations.TryGetValue(actorId, out var operation))
            {
                prefab = operation.Result;
                return true;
            }

            string location;
            try
            {
                location = GetActorLocation(actorId);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"[Entities] {exception.Message}");
                prefab = null;
                return false;
            }

            operation = _assetService.LoadAssetSync<GameObject>(location);
            prefab = operation.Result;
            if (prefab == null)
            {
                Debug.LogError($"[Entities] Failed to load actor prefab. ActorId: {actorId}, Location: {location}");
                operation.Release();
                return false;
            }

            _actorOperations.Add(actorId, operation);
            return true;
        }

        private void CleanupSceneState()
        {
            _entitiesByCharacterId.Clear();
            _world.Cleanup();
            RemoveEntityRoot();
            ReleaseActorOperations();
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
        }

        private bool SpawnCharacter(CharacterEntity entity, int actorId)
        {
            if (_root == null)
            {
                throw new InvalidOperationException("EntitySystem is not ready.");
            }

            var characterId = entity.State.CharacterId;
            if (_entitiesByCharacterId.ContainsKey(characterId))
            {
                throw new InvalidOperationException($"Character [{characterId}] has already been bound to an entity.");
            }

            if (!TryGetOrLoadActorPrefab(actorId, out var prefab))
            {
                return false;
            }

            var spatial = entity.Spatial;
            var position = spatial.Position;
            var root = UObject.Instantiate(prefab, new Vector3(position.x, position.y, 0f), spatial.Rotation, _root).transform;

            if (!root.TryGetComponent<CharacterActor>(out var actor))
            {
                actor = root.gameObject.AddComponent<CharacterActor>();
            }

            _world.AddEntity(entity, root);

#if UNITY_EDITOR
            root.name = $"{entity.GetType().Name} [EntityId: {entity.EntityId}, CharacterId: {characterId}]";
#endif

            actor.Bind(entity);
            _entitiesByCharacterId.Add(characterId, entity);
            return true;
        }
    }
}
