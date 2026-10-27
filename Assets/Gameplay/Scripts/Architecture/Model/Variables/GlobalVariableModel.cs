using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Storage;
using UnityEngine;

namespace Gameplay
{
    public sealed class GlobalVariableModel : ModelBase
    {
        [Serializable]
        private sealed class VariablesFile
        {
            public IntValue[] ints;
            public BoolValue[] bools;
        }

        [Serializable]
        private struct IntValue
        {
            public int id;
            public int value;
        }

        [Serializable]
        private struct BoolValue
        {
            public int id;
            public bool value;
        }

        private const string VariablesPath = "Variables/global.json";

        private readonly VariableStore _values = new();
        private IStorageService _storageService;
        private bool _isDirty;

        protected override void OnInitialize()
        {
            _storageService = this.GetService<IStorageService>();
        }

        protected override void OnShutdown()
        {
            try
            {
                SaveIfDirty();
            }
            finally
            {
                _storageService = null;
                _isDirty = false;
                _values.Clear();
            }
        }

        public int GetInt(int id, int defaultValue) => _values.GetInt(id, defaultValue);

        public void SetInt(int id, int value)
        {
            if (_values.SetInt(id, value))
            {
                _isDirty = true;
                this.PublishEvent(new GlobalIntVariableChangedEvent());
            }
        }

        public void ClearInt(int id)
        {
            if (_values.ClearInt(id))
            {
                _isDirty = true;
                this.PublishEvent(new GlobalIntVariableChangedEvent());
            }
        }

        public bool GetBool(int id, bool defaultValue) => _values.GetBool(id, defaultValue);

        public void SetBool(int id, bool value)
        {
            if (_values.SetBool(id, value))
            {
                _isDirty = true;
                this.PublishEvent(new GlobalBoolVariableChangedEvent());
            }
        }

        public void ClearBool(int id)
        {
            if (_values.ClearBool(id))
            {
                _isDirty = true;
                this.PublishEvent(new GlobalBoolVariableChangedEvent());
            }
        }

        public void Clear()
        {
            var changes = _values.Clear();
            _isDirty |= changes.IntsChanged || changes.BoolsChanged;
            PublishChanges(changes);
        }

        public VariableSaveData Capture() => _values.Capture();

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = new VariableSaveData();
            if (_storageService.Exists(VariablesPath))
            {
                var json = await _storageService.ReadTextAsync(VariablesPath, cancellationToken);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }
                var file = JsonUtility.FromJson<VariablesFile>(json);
                if (file == null || file.ints == null || file.bools == null)
                {
                    return;
                }
                foreach (var pair in file.ints)
                {
                    data.IntValues.Add(pair.id, pair.value);
                }
                foreach (var pair in file.bools)
                {
                    data.BoolValues.Add(pair.id, pair.value);
                }
            }

            var changes = _values.Restore(data);
            _isDirty = false;
            PublishChanges(changes);
        }

        public void SaveIfDirty()
        {
            if (!_isDirty)
            {
                return;
            }
            var data = _values.Capture();
            var ints = new IntValue[data.IntValues.Count];
            var bools = new BoolValue[data.BoolValues.Count];
            var index = 0;
            foreach (var pair in data.IntValues)
            {
                ints[index++] = new IntValue { id = pair.Key, value = pair.Value };
            }
            index = 0;
            foreach (var pair in data.BoolValues)
            {
                bools[index++] = new BoolValue { id = pair.Key, value = pair.Value };
            }
            var json = JsonUtility.ToJson(new VariablesFile { ints = ints, bools = bools }, true);
            _storageService.WriteText(VariablesPath, json, StorageWriteMode.Atomic);
            _isDirty = false;
        }

        private void PublishChanges((bool IntsChanged, bool BoolsChanged) changes)
        {
            if (changes.IntsChanged)
            {
                this.PublishEvent(new GlobalIntVariableChangedEvent());
            }
            if (changes.BoolsChanged)
            {
                this.PublishEvent(new GlobalBoolVariableChangedEvent());
            }
        }
    }
}
