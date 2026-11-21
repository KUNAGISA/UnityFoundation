using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aoiro;
using UnityEngine;

namespace Gameplay
{
    public sealed class SpawnPointModel : ModelBase
    {
        private readonly Dictionary<int, SpawnPointData> _spawnPoints = new();
        private int _defaultSpawnPointId;

        public ReadOnlyDictionary<int, SpawnPointData> SpawnPoints { get; }
        public bool HasDefault { get; private set; }

        public SpawnPointModel()
        {
            SpawnPoints = new ReadOnlyDictionary<int, SpawnPointData>(_spawnPoints);
        }

        protected override void OnInitialize()
        {
            Clear();
        }

        protected override void OnShutdown()
        {
            Clear();
        }

        public bool TryGet(int spawnPointId, out SpawnPointData spawnPoint)
        {
            return _spawnPoints.TryGetValue(spawnPointId, out spawnPoint);
        }

        public bool TryGetDefault(out SpawnPointData spawnPoint)
        {
            spawnPoint = default;
            return HasDefault && _spawnPoints.TryGetValue(_defaultSpawnPointId, out spawnPoint);
        }

        internal bool TryAdd(in SpawnPointData spawnPoint, bool isDefault)
        {
            if (!_spawnPoints.TryAdd(spawnPoint.SpawnPointId, spawnPoint))
            {
                Debug.LogError($"[Scene] 重复出生点 id [{spawnPoint.SpawnPointId}]，已跳过");
                return false;
            }

            if (!isDefault)
            {
                return true;
            }

            if (HasDefault)
            {
                Debug.LogError($"[Scene] 存在多个默认出生点，保留出生点 [{_defaultSpawnPointId}]");
                return true;
            }

            HasDefault = true;
            _defaultSpawnPointId = spawnPoint.SpawnPointId;
            return true;
        }

        internal void Clear()
        {
            _spawnPoints.Clear();
            _defaultSpawnPointId = default;
            HasDefault = false;
        }
    }
}
