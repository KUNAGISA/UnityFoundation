using Aoiro;

namespace Gameplay
{
    public sealed class GameValueModel : ModelBase
    {
        private readonly ValueStore _values = new();

        protected override void OnShutdown()
        {
            _values.Clear();
        }

        public int GetVariable(int id, int defaultValue) => _values.GetVariable(id, defaultValue);

        public void SetVariable(int id, int value)
        {
            if (_values.SetVariable(id, value))
            {
                this.PublishEvent(new GameVariableChangedEvent());
            }
        }

        public void ClearVariable(int id)
        {
            if (_values.ClearVariable(id))
            {
                this.PublishEvent(new GameVariableChangedEvent());
            }
        }

        public bool GetSwitch(int id, bool defaultValue) => _values.GetSwitch(id, defaultValue);

        public void SetSwitch(int id, bool value)
        {
            if (_values.SetSwitch(id, value))
            {
                this.PublishEvent(new GameSwitchChangedEvent());
            }
        }

        public void ClearSwitch(int id)
        {
            if (_values.ClearSwitch(id))
            {
                this.PublishEvent(new GameSwitchChangedEvent());
            }
        }

        public void ClearVariables()
        {
            if (_values.ClearVariables())
            {
                this.PublishEvent(new GameVariableChangedEvent());
            }
        }

        public void ClearSwitches()
        {
            if (_values.ClearSwitches())
            {
                this.PublishEvent(new GameSwitchChangedEvent());
            }
        }

        public void Clear()
        {
            PublishChanges(_values.Clear());
        }

        public void Restore(ValueSaveData data)
        {
            if (data?.Variables == null || data.Switches == null)
            {
                return;
            }
            PublishChanges(_values.Restore(data));
        }

        public ValueSaveData Capture() => _values.Capture();

        private void PublishChanges((bool VariablesChanged, bool SwitchesChanged) changes)
        {
            if (changes.VariablesChanged)
            {
                this.PublishEvent(new GameVariableChangedEvent());
            }
            if (changes.SwitchesChanged)
            {
                this.PublishEvent(new GameSwitchChangedEvent());
            }
        }
    }
}
