using Aoiro;
using Aoiro.Sequence;
using Gameplay.Common;

namespace Gameplay.Sequence
{
    public sealed class AdvChoiceSession : SequenceModule<ISequenceContext>
    {
        private readonly Signal _changed = new Signal();
        private VersionGate _choiceVersion;
        private string[] _choiceKeys;
        private int? _selectedChoiceIndex;
        private LocalizationSystem _localizationSystem;
        private SubscriptionToken _localizationChangedToken;

        public int Count => _choiceKeys?.Length ?? 0;
        public AdvChoiceToken Token { get; private set; }
        public ISignal Changed => _changed;

        protected override SequencePreparation OnPrepare()
        {
            _localizationSystem = Player.Context.GetSystem<LocalizationSystem>();
            _localizationChangedToken = _localizationSystem.CatalogChanged.Subscribe(OnLocalizationCatalogChanged);
            return null;
        }

        protected override void OnCleanup()
        {
            _localizationChangedToken.Cancel();
            _localizationChangedToken = default;
            _localizationSystem = null;
            _changed.Clear();
            _choiceKeys = null;
            _selectedChoiceIndex = null;
        }

        public AdvChoiceToken Show(int choiceId, string[] choiceKeys)
        {
            _choiceKeys = choiceKeys;
            _selectedChoiceIndex = null;
            Token = new AdvChoiceToken(choiceId, _choiceVersion.Bump());
            _changed.Emit();
            return Token;
        }

        public string GetText(int index)
        {
            return _localizationSystem.Get(_choiceKeys[index], TextSourceFlags.Global | TextSourceFlags.Sequence);
        }

        public void Select(in AdvChoiceToken token, int index)
        {
            if (IsCurrent(token) && index >= 0 && index < Count)
            {
                _selectedChoiceIndex = index;
            }
        }

        public bool TryGetSelectedIndex(in AdvChoiceToken token, out int index)
        {
            index = _selectedChoiceIndex ?? 0;
            return IsCurrent(token) && _selectedChoiceIndex.HasValue;
        }

        public void Hide(in AdvChoiceToken token)
        {
            if (IsCurrent(token))
            {
                _choiceKeys = null;
                _changed.Emit();
            }
        }

        private bool IsCurrent(in AdvChoiceToken token) => token.Equals(Token);

        private void OnLocalizationCatalogChanged(LocalizationCatalogChanged change)
        {
            if (_choiceKeys != null && (change.Sources & (TextSourceFlags.Global | TextSourceFlags.Sequence)) != 0)
            {
                _changed.Emit();
            }
        }
    }
}
