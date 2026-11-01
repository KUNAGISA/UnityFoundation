using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Storage;
using UnityEngine;

namespace Gameplay
{
    public sealed class GlobalValueModel : ModelBase
    {
        [Serializable]
        private sealed class VariablesAndSwitchesFile
        {
            // 保留既有 JSON 键，兼容已保存的全局变量与开关。
            public VariableValue[] ints;
            public SwitchValue[] bools;
        }

        [Serializable]
        private struct VariableValue
        {
            public int id;
            public int value;
        }

        [Serializable]
        private struct SwitchValue
        {
            public int id;
            public bool value;
        }

        private const string VariablesAndSwitchesPath = "Variables/global.json";

        private readonly ValueStore _values = new();
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

        public int GetVariable(int id, int defaultValue) => _values.GetVariable(id, defaultValue);

        public void SetVariable(int id, int value)
        {
            if (_values.SetVariable(id, value))
            {
                _isDirty = true;
                this.PublishEvent(new GlobalVariableChangedEvent());
            }
        }

        public void ClearVariable(int id)
        {
            if (_values.ClearVariable(id))
            {
                _isDirty = true;
                this.PublishEvent(new GlobalVariableChangedEvent());
            }
        }

        public bool GetSwitch(int id, bool defaultValue) => _values.GetSwitch(id, defaultValue);

        public void SetSwitch(int id, bool value)
        {
            if (_values.SetSwitch(id, value))
            {
                _isDirty = true;
                this.PublishEvent(new GlobalSwitchChangedEvent());
            }
        }

        public void ClearSwitch(int id)
        {
            if (_values.ClearSwitch(id))
            {
                _isDirty = true;
                this.PublishEvent(new GlobalSwitchChangedEvent());
            }
        }

        public void ClearVariables()
        {
            if (_values.ClearVariables())
            {
                _isDirty = true;
                this.PublishEvent(new GlobalVariableChangedEvent());
            }
        }

        public void ClearSwitches()
        {
            if (_values.ClearSwitches())
            {
                _isDirty = true;
                this.PublishEvent(new GlobalSwitchChangedEvent());
            }
        }

        public void Clear()
        {
            var changes = _values.Clear();
            _isDirty |= changes.VariablesChanged || changes.SwitchesChanged;
            PublishChanges(changes);
        }

        public ValueSaveData Capture() => _values.Capture();

        public async UniTask LoadAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = new ValueSaveData();
            if (_storageService.Exists(VariablesAndSwitchesPath))
            {
                var json = await _storageService.ReadTextAsync(VariablesAndSwitchesPath, cancellationToken);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }
                var file = JsonUtility.FromJson<VariablesAndSwitchesFile>(json);
                if (file == null || file.ints == null || file.bools == null)
                {
                    return;
                }
                foreach (var pair in file.ints)
                {
                    data.Variables.Add(pair.id, pair.value);
                }
                foreach (var pair in file.bools)
                {
                    data.Switches.Add(pair.id, pair.value);
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
            var variables = new VariableValue[data.Variables.Count];
            var switches = new SwitchValue[data.Switches.Count];
            var index = 0;
            foreach (var pair in data.Variables)
            {
                variables[index++] = new VariableValue { id = pair.Key, value = pair.Value };
            }
            index = 0;
            foreach (var pair in data.Switches)
            {
                switches[index++] = new SwitchValue { id = pair.Key, value = pair.Value };
            }
            var json = JsonUtility.ToJson(new VariablesAndSwitchesFile { ints = variables, bools = switches }, true);
            _storageService.WriteText(VariablesAndSwitchesPath, json, StorageWriteMode.Atomic);
            _isDirty = false;
        }

        private void PublishChanges((bool VariablesChanged, bool SwitchesChanged) changes)
        {
            if (changes.VariablesChanged)
            {
                this.PublishEvent(new GlobalVariableChangedEvent());
            }
            if (changes.SwitchesChanged)
            {
                this.PublishEvent(new GlobalSwitchChangedEvent());
            }
        }
    }
}
