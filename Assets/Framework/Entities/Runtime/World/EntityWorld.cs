using System;
using System.Collections.Generic;
using Gameplay.Common;
using UnityEngine;

namespace Gameplay.Entities
{
    public sealed class EntityWorld
    {
        public bool HasEntities => _entitiesById.Count > 0;

        private readonly EntityContext _context;
        private readonly Dictionary<long, Entity> _entitiesById = new();
        private readonly List<Entity> _activeEntityList = new();
        private readonly IEntityReleaser _releaser;
        private long _nextEntityId;
        private int _activeEntityCount;
        private int _pendingRemovalCount;

        public EntityWorld(EntityContext context, IEntityReleaser releaser)
        {
            _context = context;
            _releaser = releaser;
        }

        public void Cleanup()
        {
            _activeEntityList.Clear();
            var exceptions = new CleanupExceptionCollector();
            foreach (var (_, entity) in _entitiesById)
            {
                DestroyAndRelease(entity, EntityReleaseContext.Create(entity), exceptions);
            }
            _entitiesById.Clear();
            _activeEntityCount = 0;
            _pendingRemovalCount = 0;
            exceptions.ThrowIfAny();
        }

        public void AddEntity(Entity entity, Transform root)
        {
            if (entity == null || root == null)
            {
                _releaser.Release(new EntityReleaseContext(entity, root));
                throw new ArgumentException("Entity and root must not be null.");
            }

            if (!entity.CanInitialize)
            {
                _releaser.Release(new EntityReleaseContext(entity, root));
                throw new InvalidOperationException($"Entity [{entity.GetType().Name}] has already been added to a world.");
            }

            if (_nextEntityId == long.MaxValue)
            {
                _releaser.Release(new EntityReleaseContext(entity, root));
                throw new InvalidOperationException("Entity id has reached its maximum value.");
            }

            var entityId = ++_nextEntityId;
            _entitiesById.Add(entityId, entity);

            try
            {
                entity.Initialize(_context, entityId, root);
            }
            catch (Exception initializeException)
            {
                _entitiesById.Remove(entityId);
                var exceptions = new CleanupExceptionCollector();
                exceptions.Add(initializeException);

                DestroyAndRelease(entity, new EntityReleaseContext(entity, root), exceptions);

                exceptions.ThrowIfAny();
                throw;
            }

            var index = _activeEntityCount++;
            if (_activeEntityList.Count == index)
            {
                _activeEntityList.Add(entity);
            }
            else
            {
                _activeEntityList[index] = entity;
            }
        }

        public void RemoveEntity(long entityId)
        {
            if (!_entitiesById.TryGetValue(entityId, out var entity))
            {
                Debug.LogError($"[Entities] 找不到Entity [{entityId}]");
                return;
            }

            if (entity.TryMarkPendingRemoval())
            {
                _pendingRemovalCount++;
            }
        }

        public bool TryGetEntity(long entityId, out Entity entity)
        {
            if (_entitiesById.TryGetValue(entityId, out entity) && entity.IsValid)
            {
                return true;
            }

            entity = null;
            return false;
        }

        public void FrameTick(float deltaTime)
        {
            var count = _activeEntityCount;
            for (var index = 0; index < count; index++)
            {
                var entity = _activeEntityList[index];
                if (entity.IsValid)
                {
                    entity.FrameTick(deltaTime);
                }
            }
        }

        public void LateTick(float deltaTime)
        {
            var count = _activeEntityCount;
            if (count == 0)
            {
                return;
            }

            for (var index = 0; index < count; index++)
            {
                var entity = _activeEntityList[index];
                if (entity.IsValid)
                {
                    entity.LateTick(deltaTime);
                }
            }

            if (_pendingRemovalCount > 0)
            {
                FlushPendingRemovals();
            }
        }

        private void FlushPendingRemovals()
        {
            var write = 0;
            var count = _activeEntityCount;
            var exceptions = new CleanupExceptionCollector();

            for (var read = 0; read < count; read++)
            {
                var entity = _activeEntityList[read];
                if (entity.IsPendingRemoval)
                {
                    --_pendingRemovalCount;
                    _entitiesById.Remove(entity.EntityId);

                    DestroyAndRelease(entity, EntityReleaseContext.Create(entity), exceptions);

                    _activeEntityList[read] = null;
                    continue;
                }

                if (write != read)
                {
                    _activeEntityList[write] = entity;
                    _activeEntityList[read] = null;
                }

                write++;
            }

            for (var i = write; i < count; i++)
            {
                _activeEntityList[i] = null;
            }

            _activeEntityCount = write;
            exceptions.ThrowIfAny();
        }

        private void DestroyAndRelease(Entity entity, in EntityReleaseContext context, CleanupExceptionCollector exceptions)
        {
            try
            {
                entity.Destroy();
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }

            try
            {
                _releaser.Release(context);
            }
            catch (Exception exception)
            {
                exceptions.Add(exception);
            }
        }
    }
}
