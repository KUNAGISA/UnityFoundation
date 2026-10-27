using Aoiro;

namespace Gameplay
{
    public sealed class GameVariableModel : ModelBase
    {
        private readonly VariableStore _values = new();

        protected override void OnShutdown()
        {
            _values.Clear();
        }

        public int GetInt(int id, int defaultValue) => _values.GetInt(id, defaultValue);

        public void SetInt(int id, int value)
        {
            if (_values.SetInt(id, value))
            {
                this.PublishEvent(new GameIntVariableChangedEvent());
            }
        }

        public void ClearInt(int id)
        {
            if (_values.ClearInt(id))
            {
                this.PublishEvent(new GameIntVariableChangedEvent());
            }
        }

        public bool GetBool(int id, bool defaultValue) => _values.GetBool(id, defaultValue);

        public void SetBool(int id, bool value)
        {
            if (_values.SetBool(id, value))
            {
                this.PublishEvent(new GameBoolVariableChangedEvent());
            }
        }

        public void ClearBool(int id)
        {
            if (_values.ClearBool(id))
            {
                this.PublishEvent(new GameBoolVariableChangedEvent());
            }
        }

        public void Clear()
        {
            PublishChanges(_values.Clear());
        }

        public void Restore(VariableSaveData data)
        {
            if (data?.IntValues == null || data.BoolValues == null)
            {
                return;
            }
            PublishChanges(_values.Restore(data));
        }

        public VariableSaveData Capture() => _values.Capture();

        private void PublishChanges((bool IntsChanged, bool BoolsChanged) changes)
        {
            if (changes.IntsChanged)
            {
                this.PublishEvent(new GameIntVariableChangedEvent());
            }
            if (changes.BoolsChanged)
            {
                this.PublishEvent(new GameBoolVariableChangedEvent());
            }
        }
    }
}
