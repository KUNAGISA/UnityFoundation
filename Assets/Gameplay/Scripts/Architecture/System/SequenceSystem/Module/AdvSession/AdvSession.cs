using System;
using Aoiro;
using Aoiro.Sequence;
using Gameplay.Common;
using Gameplay.Tables;
using Gameplay.UI;

namespace Gameplay.Sequence
{
    public sealed class AdvSession : SequenceModule<ISequenceContext>
    {
        private enum ViewState
        {
            None,
            Opening,
            Bound,
            Closed,
            Cleaning
        }

        private readonly Signal _choicesChanged = new Signal();
        private VersionGate _choiceVersion;
        private UIViewToken _viewToken = UIViewToken.None;
        private AdvChoiceToken _choiceToken;
        private int? _selectedChoiceIndex;
        private ViewState _viewState;
        private bool _advanceRequested;
        private LocalizationSystem _localizationSystem;
        private SubscriptionToken _localizationChangedToken;

        public AdvDialogue Dialogue { get; } = new AdvDialogue();
        public AdvPortraitRegistry Portraits { get; } = new AdvPortraitRegistry();
        public string[] Choices { get; private set; }
        public bool ChoicesVisible => Choices != null;
        public AdvChoiceToken ChoiceToken => _choiceToken;
        public bool IsViewBound => _viewState == ViewState.Bound;
        public ISignal ChoicesChanged => _choicesChanged;

        protected override SequencePreparation OnPrepare()
        {
            var uiSystem = Player.Context.GetSystem<UISystem>();
            _localizationSystem = Player.Context.GetSystem<LocalizationSystem>();
            _localizationChangedToken = _localizationSystem.CatalogChanged.Subscribe(OnLocalizationCatalogChanged);
            _viewState = ViewState.Opening;
            _viewToken = uiSystem.OpenView(new AdvView.Data(this));
            _advanceRequested = false;
            return new AdvViewPreparation(this, _viewToken);
        }

        protected override void OnTick(float deltaTime)
        {
            if (_viewState == ViewState.Closed)
            {
                throw new InvalidOperationException($"AdvView [{_viewToken.ViewId}] was closed during sequence playback.");
            }
            if (_advanceRequested)
            {
                _advanceRequested = false;
                Player.Advance();
            }
        }

        protected override void OnCleanup()
        {
            _viewState = ViewState.Cleaning;
            var localizationToken = _localizationChangedToken;
            var viewToken = _viewToken;
            _localizationChangedToken = default;
            _viewToken = UIViewToken.None;
            _localizationSystem = null;

            localizationToken.Cancel();
            viewToken.Cancel();
            _choicesChanged.Clear();
            Dialogue.Clear();
            Portraits.Clear();
            Choices = null;
            _selectedChoiceIndex = null;
            _advanceRequested = false;
        }

        public void NotifyViewBound()
        {
            if (_viewState != ViewState.Cleaning)
            {
                _viewState = ViewState.Bound;
            }
        }

        public void NotifyViewClosed()
        {
            if (_viewState != ViewState.Cleaning)
            {
                _viewState = ViewState.Closed;
            }
        }

        public void RequestAdvance() => _advanceRequested = true;

        public VersionToken BeginDialogue(string textKey, string characterNameKey, bool autoAdvance)
        {
            textKey ??= string.Empty;
            characterNameKey ??= string.Empty;
            var text = _localizationSystem.Get(textKey, TextSourceFlags.Sequence | TextSourceFlags.Global);
            var characterName = ResolveCharacterName(characterNameKey);
            var settings = Player.Context.GetModel<TablesModel>().SequenceSettingsTable;
            return Dialogue.Begin(textKey, characterNameKey, text, characterName, settings.MessageCharIntervalMs, settings.MessageAdvanceableWaitTimeMs, autoAdvance);
        }

        public AdvChoiceToken ShowChoices(int choiceId, string[] choices)
        {
            Choices = choices;
            _selectedChoiceIndex = null;
            _choiceToken = new AdvChoiceToken(choiceId, _choiceVersion.Bump());
            _choicesChanged.Emit();
            return _choiceToken;
        }

        public void SelectChoice(in AdvChoiceToken token, int index)
        {
            if (IsCurrentChoice(token) && Choices != null && index >= 0 && index < Choices.Length)
            {
                _selectedChoiceIndex = index;
            }
        }

        public bool TryGetSelectedChoice(in AdvChoiceToken token, out int index)
        {
            index = _selectedChoiceIndex ?? 0;
            return IsCurrentChoice(token) && _selectedChoiceIndex.HasValue;
        }

        public void HideChoices(in AdvChoiceToken token)
        {
            if (IsCurrentChoice(token))
            {
                Choices = null;
                _choicesChanged.Emit();
            }
        }

        private bool IsCurrentChoice(in AdvChoiceToken token) => token.ChoiceId == _choiceToken.ChoiceId && token.Version == _choiceVersion.Current;

        private void OnLocalizationCatalogChanged(LocalizationCatalogChanged change)
        {
            if ((change.Sources & (TextSourceFlags.Global | TextSourceFlags.Sequence)) == 0)
            {
                return;
            }
            if (Dialogue.HasDialogue)
            {
                var text = _localizationSystem.Get(Dialogue.TextKey, TextSourceFlags.Sequence | TextSourceFlags.Global);
                Dialogue.UpdateLocalizedContent(text, ResolveCharacterName(Dialogue.CharacterNameKey));
            }
            if (ChoicesVisible)
            {
                _choicesChanged.Emit();
            }
        }

        private string ResolveCharacterName(string key)
        {
            return string.IsNullOrEmpty(key) ? string.Empty : _localizationSystem.Get(key, TextSourceFlags.Sequence | TextSourceFlags.Global);
        }
    }
}
