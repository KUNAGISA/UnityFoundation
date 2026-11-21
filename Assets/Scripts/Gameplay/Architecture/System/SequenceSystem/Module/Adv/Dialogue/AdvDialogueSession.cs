using System;
using Aoiro;
using Aoiro.Sequence;
using Gameplay.Common;
using Gameplay.Tables;

namespace Gameplay.Sequence
{
    public sealed class AdvDialogueSession : SequenceModule<ISequenceContext>
    {
        private readonly Signal<AdvDialogueChangeFlags> _changed = new Signal<AdvDialogueChangeFlags>();
        private LocalizationSystem _localizationSystem;
        private SubscriptionToken _localizationChangedToken;
        private VersionGate _version;
        private VersionGate _contentRevision;
        private double _characterIntervalMs;
        private double _advanceWaitMs;
        private double _elapsedMs;
        private double _durationMs;
        private int _characterCount;
        private bool _autoAdvance;
        private bool _measurementReady;
        private bool _revealCompleted;
        private bool _hasDialogue;
        private string _textKey = string.Empty;
        private string _characterNameKey = string.Empty;

        public bool IsCompleted { get; private set; }
        public string Text { get; private set; } = string.Empty;
        public string CharacterName { get; private set; } = string.Empty;
        public int VisibleCharacterCount { get; private set; }
        public bool AdvancePromptVisible { get; private set; }
        public VersionToken Version => _version.Current;
        public VersionToken ContentRevision => _contentRevision.Current;
        public ISignal<AdvDialogueChangeFlags> Changed => _changed;

        protected override SequencePreparation OnPrepare()
        {
            _localizationSystem = Player.Context.GetSystem<LocalizationSystem>();
            _localizationChangedToken = _localizationSystem.CatalogChanged.Subscribe(OnLocalizationCatalogChanged);
            return null;
        }

        public VersionToken Begin(string textKey, string characterNameKey, bool autoAdvance)
        {
            textKey ??= string.Empty;
            characterNameKey ??= string.Empty;
            var text = _localizationSystem.Get(textKey, TextSourceFlags.Sequence | TextSourceFlags.Global);
            var characterName = ResolveCharacterName(characterNameKey);
            var settings = Player.Context.GetModel<TablesModel>().SequenceSettingsTable;
            _hasDialogue = true;
            _textKey = textKey;
            _characterNameKey = characterNameKey;
            Text = text;
            CharacterName = characterName;
            _characterIntervalMs = settings.MessageCharIntervalMs;
            _advanceWaitMs = settings.MessageAdvanceableWaitTimeMs;
            _autoAdvance = autoAdvance;
            ResetProgress();
            var version = _version.Bump();
            _contentRevision.Bump();
            _changed.Emit(AdvDialogueChangeFlags.All);
            return version;
        }

        private void UpdateLocalizedContent(string text, string characterName)
        {
            var completed = IsCompleted;
            Text = text;
            CharacterName = characterName;
            ResetProgress();
            IsCompleted = completed;
            // 已结束的 Action 不再 Tick，换语言后直接完整显示新译文。
            VisibleCharacterCount = completed ? int.MaxValue : 0;
            AdvancePromptVisible = completed;
            _contentRevision.Bump();
            _changed.Emit(AdvDialogueChangeFlags.All);
        }

        public void ReportCharacterCount(in VersionToken version, in VersionToken contentRevision, int characterCount)
        {
            if (!IsCurrent(version) || contentRevision != ContentRevision)
            {
                return;
            }
            _characterCount = characterCount;
            _durationMs = characterCount * _characterIntervalMs + _advanceWaitMs;
            _measurementReady = true;
            if (IsCompleted)
            {
                VisibleCharacterCount = characterCount;
            }
        }

        public void Tick(in VersionToken version, float deltaTime)
        {
            if (!IsCurrent(version) || IsCompleted || !_measurementReady)
            {
                return;
            }
            _elapsedMs += deltaTime * 1000f;
            var changed = AdvDialogueChangeFlags.None;
            if (!_revealCompleted)
            {
                var visibleCharacterCount = Math.Min((int)Math.Floor(_elapsedMs / _characterIntervalMs), _characterCount);
                if (VisibleCharacterCount != visibleCharacterCount)
                {
                    VisibleCharacterCount = visibleCharacterCount;
                    changed |= AdvDialogueChangeFlags.Text;
                }
                if (visibleCharacterCount >= _characterCount)
                {
                    _revealCompleted = true;
                    AdvancePromptVisible = true;
                    changed |= AdvDialogueChangeFlags.AdvancePrompt;
                }
            }
            if (_autoAdvance && _elapsedMs >= _durationMs)
            {
                IsCompleted = true;
            }
            if (changed != AdvDialogueChangeFlags.None)
            {
                _changed.Emit(changed);
            }
        }

        public void Advance(in VersionToken version)
        {
            if (!IsCurrent(version) || IsCompleted || !_measurementReady)
            {
                return;
            }
            // 保留原推进规则：未到允许继续的时点时，先补全文字并跳过等待。
            IsCompleted = _elapsedMs >= _durationMs;
            _elapsedMs = _durationMs;
            _revealCompleted = true;
            VisibleCharacterCount = _characterCount;
            AdvancePromptVisible = true;
            _changed.Emit(AdvDialogueChangeFlags.Text | AdvDialogueChangeFlags.AdvancePrompt);
        }

        public bool IsCurrent(in VersionToken version)
        {
            return _hasDialogue && version == Version;
        }

        protected override void OnCleanup()
        {
            _localizationChangedToken.Cancel();
            _localizationChangedToken = default;
            _localizationSystem = null;
            _changed.Clear();
            _hasDialogue = false;
            _textKey = string.Empty;
            _characterNameKey = string.Empty;
            Text = string.Empty;
            CharacterName = string.Empty;
            ResetProgress();
        }

        private void OnLocalizationCatalogChanged(LocalizationCatalogChanged change)
        {
            if (_hasDialogue && (change.Sources & (TextSourceFlags.Global | TextSourceFlags.Sequence)) != 0)
            {
                var text = _localizationSystem.Get(_textKey, TextSourceFlags.Sequence | TextSourceFlags.Global);
                UpdateLocalizedContent(text, ResolveCharacterName(_characterNameKey));
            }
        }

        private string ResolveCharacterName(string key)
        {
            return string.IsNullOrEmpty(key) ? string.Empty : _localizationSystem.Get(key, TextSourceFlags.Sequence | TextSourceFlags.Global);
        }

        private void ResetProgress()
        {
            _elapsedMs = 0;
            _durationMs = 0;
            _characterCount = 0;
            _measurementReady = false;
            _revealCompleted = false;
            IsCompleted = false;
            VisibleCharacterCount = 0;
            AdvancePromptVisible = false;
        }
    }
}
