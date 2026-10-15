using System;
using System.Collections.Generic;
using Aoiro;
using Aoiro.Sequence;
using Gameplay.Common;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay.Sequence
{
    public sealed class AdvSession : SequenceModule<ISequenceContext>
    {
        private enum PresentationState
        {
            None,
            Opening,
            Attached,
            Released,
            Cleaning
        }

        private readonly Signal<AdvChange> _changed = new Signal<AdvChange>();
        private readonly Signal<AdvPortraitChange> _portraitChanged = new Signal<AdvPortraitChange>();
        private readonly AdvPortraitRegistry _portraits = new AdvPortraitRegistry();
        private VersionGate _dialogueVersion;
        private VersionGate _dialogueContentRevision;
        private VersionGate _choiceVersion;
        private UIViewToken _viewToken = UIViewToken.None;
        private int? _dialogueCharacterCount;
        private AdvChoiceToken _choiceToken;
        private int? _selectedChoiceIndex;
        private PresentationState _presentationState;
        private bool _advanceRequested;
        private bool _hasDialogue;
        private string _dialogueTextKey = string.Empty;
        private string _characterNameKey = string.Empty;
        private LocalizationSystem _localizationSystem;
        private SubscriptionToken _localizationChangedToken;

        public string DialogueText { get; private set; } = string.Empty;
        public int DialogueVisibleCount { get; private set; }
        public VersionToken DialogueVersion => _dialogueVersion.Current;
        public VersionToken DialogueContentRevision => _dialogueContentRevision.Current;
        public string CharacterName { get; private set; } = string.Empty;
        public bool AdvancePromptVisible { get; private set; }
        public string[] Choices { get; private set; }
        public bool ChoicesVisible => Choices != null;
        public AdvChoiceToken ChoiceToken => _choiceToken;
        public IReadOnlyDictionary<int, AdvPortraitEntry> Portraits => _portraits.Entries;
        public bool IsPresentationAttached => _presentationState == PresentationState.Attached;

        public ISignal<AdvChange> Changed => _changed;
        public ISignal<AdvPortraitChange> PortraitChanged => _portraitChanged;

        protected override SequencePreparation OnPrepare()
        {
            var uiSystem = Player.Context.GetSystem<UISystem>();
            _localizationSystem = Player.Context.GetSystem<LocalizationSystem>();
            _localizationChangedToken = _localizationSystem.CatalogChanged.Subscribe(OnLocalizationCatalogChanged);
            _presentationState = PresentationState.Opening;
            _viewToken = uiSystem.OpenView(new AdvView.Data(this));
            _advanceRequested = false;
            return new AdvViewPreparation(this, _viewToken);
        }

        protected override void OnTick(float deltaTime)
        {
            if (_presentationState == PresentationState.Released)
            {
                throw new InvalidOperationException($"AdvView [{_viewToken.ViewId}] was released during sequence playback.");
            }

            if (_advanceRequested)
            {
                _advanceRequested = false;
                Player.Advance();
            }
        }

        protected override void OnCleanup()
        {
            _presentationState = PresentationState.Cleaning;

            _localizationChangedToken.Cancel();
            _localizationChangedToken = default;
            _localizationSystem = null;

            _viewToken.Cancel();
            _viewToken = UIViewToken.None;

            _changed.Clear();
            _portraitChanged.Clear();
            _portraits.Clear();

            _advanceRequested = false;
            _hasDialogue = false;
            _dialogueTextKey = string.Empty;
            _characterNameKey = string.Empty;
        }

        public void AttachPresentation()
        {
            if (_presentationState != PresentationState.Cleaning)
            {
                _presentationState = PresentationState.Attached;
            }
        }

        public void DetachPresentation()
        {
            if (_presentationState != PresentationState.Cleaning)
            {
                _presentationState = PresentationState.Released;
            }
        }

        public void RequestAdvance() => _advanceRequested = true;

        public VersionToken SetDialogue(string textKey, string characterNameKey)
        {
            _hasDialogue = true;
            _dialogueTextKey = textKey ?? string.Empty;
            _characterNameKey = characterNameKey ?? string.Empty;
            RefreshLocalizedDialogue();
            DialogueVisibleCount = 0;
            _dialogueCharacterCount = null;
            AdvancePromptVisible = false;
            var version = _dialogueVersion.Bump();
            _dialogueContentRevision.Bump();
            _changed.Emit(AdvChange.Dialogue | AdvChange.CharacterName | AdvChange.AdvancePrompt);
            return version;
        }

        public void ReportDialogueMeasurement(in VersionToken token, int characterCount)
        {
            if (token == _dialogueVersion.Current)
            {
                _dialogueCharacterCount = characterCount;
            }
        }

        public bool TryGetDialogueCharacterCount(in VersionToken token, out int characterCount)
        {
            characterCount = _dialogueCharacterCount ?? 0;
            return token == _dialogueVersion.Current && _dialogueCharacterCount.HasValue;
        }

        public void SetDialogueVisibleCount(in VersionToken token, int count)
        {
            if (token == _dialogueVersion.Current)
            {
                DialogueVisibleCount = count;
                _changed.Emit(AdvChange.Dialogue);
            }
        }

        public void CompleteDialogue(in VersionToken token)
        {
            if (token == _dialogueVersion.Current && !AdvancePromptVisible)
            {
                AdvancePromptVisible = true;
                _changed.Emit(AdvChange.AdvancePrompt);
            }
        }

        public AdvChoiceToken ShowChoices(int choiceId, string[] choices)
        {
            Choices = choices;
            _selectedChoiceIndex = null;
            _choiceToken = new AdvChoiceToken(choiceId, _choiceVersion.Bump());
            _changed.Emit(AdvChange.Choices);
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
                _changed.Emit(AdvChange.Choices);
            }
        }

        public AdvPortraitChannelToken AcquirePortraitToken(int portraitId, AdvPortraitChannel channel)
        {
            return TryGetRegisteredPortrait(portraitId, out var portrait) ? portrait.Acquire(channel) : default;
        }

        public bool IsPortraitTokenValid(in AdvPortraitChannelToken token) => _portraits.IsTokenValid(token);
        public ref readonly AdvPortraitVisualState GetPortraitVisualState(int portraitId) => ref _portraits.GetVisualState(portraitId);
        public bool TryGetPortrait(int portraitId, out AdvPortraitEntry portrait) => _portraits.TryGet(portraitId, out portrait);

        public void SetPortraitAsset(int portraitId, string assetLocation, Vector2? position)
        {
            var portrait = _portraits.GetOrAdd(portraitId);
            if (position.HasValue)
            {
                portrait.VisualState.Position = position.Value;
            }

            portrait.AssetLocation = assetLocation;
            EmitPortraitChanged(portraitId, AdvPortraitChangeType.Asset);
        }

        public void SetPortraitSortingOrder(int portraitId, int order)
        {
            if (TryGetRegisteredPortrait(portraitId, out var portrait))
            {
                portrait.SortingOrder = order;
                EmitPortraitChanged(portraitId, AdvPortraitChangeType.SortingOrder);
            }
        }

        public void SetPortraitOrigin(int portraitId, Vector2 origin)
        {
            if (TryGetRegisteredPortrait(portraitId, out var portrait))
            {
                portrait.VisualState.Origin = origin;
                EmitPortraitChanged(portraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetPortraitFlipHorizontal(int portraitId, bool flip)
        {
            if (TryGetRegisteredPortrait(portraitId, out var portrait))
            {
                portrait.VisualState.HorizontalFlip = flip;
                EmitPortraitChanged(portraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetPortraitFlipVertical(int portraitId, bool flip)
        {
            if (TryGetRegisteredPortrait(portraitId, out var portrait))
            {
                portrait.VisualState.VerticalFlip = flip;
                EmitPortraitChanged(portraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetPortraitPosition(in AdvPortraitChannelToken token, Vector2 value)
        {
            if (_portraits.TryGet(token, AdvPortraitChannel.Position, out var portrait))
            {
                portrait.VisualState.Position = value;
                EmitPortraitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetPortraitOpacity(in AdvPortraitChannelToken token, float value)
        {
            if (_portraits.TryGet(token, AdvPortraitChannel.Opacity, out var portrait))
            {
                portrait.VisualState.Opacity = value;
                EmitPortraitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetPortraitTint(in AdvPortraitChannelToken token, ColorRGB value)
        {
            if (_portraits.TryGet(token, AdvPortraitChannel.Tint, out var portrait))
            {
                portrait.VisualState.Tint = value;
                EmitPortraitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetPortraitScale(in AdvPortraitChannelToken token, Vector2 value)
        {
            if (_portraits.TryGet(token, AdvPortraitChannel.Scale, out var portrait))
            {
                portrait.VisualState.Scale = value;
                EmitPortraitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetPortraitRotation(in AdvPortraitChannelToken token, float value)
        {
            if (_portraits.TryGet(token, AdvPortraitChannel.Rotation, out var portrait))
            {
                portrait.VisualState.RotationDegrees = value;
                EmitPortraitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        private bool IsCurrentChoice(in AdvChoiceToken token) => token.ChoiceId == _choiceToken.ChoiceId && token.Version == _choiceVersion.Current;
        private void EmitPortraitChanged(int portraitId, AdvPortraitChangeType type) => _portraitChanged.Emit(new AdvPortraitChange(portraitId, type));

        private void OnLocalizationCatalogChanged(LocalizationCatalogChanged change)
        {
            if ((change.Sources & (TextSourceFlags.Global | TextSourceFlags.Sequence)) == 0)
            {
                return;
            }

            var changed = AdvChange.None;
            if (_hasDialogue)
            {
                RefreshLocalizedDialogue();
                DialogueVisibleCount = 0;
                _dialogueCharacterCount = null;
                AdvancePromptVisible = false;
                _dialogueContentRevision.Bump();
                changed |= AdvChange.Dialogue | AdvChange.CharacterName | AdvChange.AdvancePrompt;
            }
            if (ChoicesVisible)
            {
                changed |= AdvChange.Choices;
            }
            if (changed != AdvChange.None)
            {
                _changed.Emit(changed);
            }
        }

        private void RefreshLocalizedDialogue()
        {
            DialogueText = _localizationSystem.Get(_dialogueTextKey, TextSourceFlags.Sequence | TextSourceFlags.Global);
            CharacterName = string.IsNullOrEmpty(_characterNameKey)
                ? string.Empty
                : _localizationSystem.Get(_characterNameKey, TextSourceFlags.Sequence | TextSourceFlags.Global);
        }

        private bool TryGetRegisteredPortrait(int portraitId, out AdvPortraitEntry portrait)
        {
            if (_portraits.TryGet(portraitId, out portrait))
            {
                return true;
            }

            Debug.LogError($"[Sequence] 未注册立绘 Id [{portraitId}]。");
            return false;
        }
    }
}
