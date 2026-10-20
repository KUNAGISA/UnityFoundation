using System;
using Aoiro;
using Gameplay.Common;

namespace Gameplay.Sequence
{
    public sealed class AdvDialogue
    {
        private readonly Signal<AdvDialogueChange> _changed = new Signal<AdvDialogueChange>();
        private VersionGate _version;
        private VersionGate _contentRevision;
        private double _characterIntervalMs;
        private double _advanceWaitMs;
        private double _elapsedMs;
        private double _durationMs;
        private int _characterCount;
        private bool _autoAdvance;
        private bool _measurementReady;
        private bool _textCompleted;
        private bool _completed;

        public bool HasDialogue { get; private set; }
        public bool IsCompleted => _completed;
        public string TextKey { get; private set; } = string.Empty;
        public string CharacterNameKey { get; private set; } = string.Empty;
        public string Text { get; private set; } = string.Empty;
        public string CharacterName { get; private set; } = string.Empty;
        public int VisibleCharacterCount { get; private set; }
        public bool AdvancePromptVisible { get; private set; }
        public VersionToken Version => _version.Current;
        public VersionToken ContentRevision => _contentRevision.Current;
        public ISignal<AdvDialogueChange> Changed => _changed;

        public VersionToken Begin(string textKey, string characterNameKey, string text, string characterName, double characterIntervalMs, double advanceWaitMs, bool autoAdvance)
        {
            HasDialogue = true;
            TextKey = textKey;
            CharacterNameKey = characterNameKey;
            Text = text;
            CharacterName = characterName;
            _characterIntervalMs = characterIntervalMs;
            _advanceWaitMs = advanceWaitMs;
            _autoAdvance = autoAdvance;
            ResetProgress();
            var version = _version.Bump();
            _contentRevision.Bump();
            _changed.Emit(AdvDialogueChange.All);
            return version;
        }

        public void UpdateLocalizedContent(string text, string characterName)
        {
            if (!HasDialogue)
            {
                return;
            }
            var completed = _completed;
            Text = text;
            CharacterName = characterName;
            ResetProgress();
            _completed = completed;
            // 已结束的 Action 不再 Tick，换语言后直接完整显示新译文。
            VisibleCharacterCount = completed ? int.MaxValue : 0;
            AdvancePromptVisible = completed;
            _contentRevision.Bump();
            _changed.Emit(AdvDialogueChange.All);
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
            if (_completed)
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
            var changed = AdvDialogueChange.None;
            if (!_textCompleted)
            {
                var visibleCharacterCount = Math.Min((int)Math.Floor(_elapsedMs / _characterIntervalMs), _characterCount);
                if (VisibleCharacterCount != visibleCharacterCount)
                {
                    VisibleCharacterCount = visibleCharacterCount;
                    changed |= AdvDialogueChange.Text;
                }
                if (visibleCharacterCount >= _characterCount)
                {
                    _textCompleted = true;
                    AdvancePromptVisible = true;
                    changed |= AdvDialogueChange.AdvancePrompt;
                }
            }
            if (_autoAdvance && _elapsedMs >= _durationMs)
            {
                _completed = true;
            }
            if (changed != AdvDialogueChange.None)
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
            _completed = _elapsedMs >= _durationMs;
            _elapsedMs = _durationMs;
            _textCompleted = true;
            VisibleCharacterCount = _characterCount;
            AdvancePromptVisible = true;
            _changed.Emit(AdvDialogueChange.Text | AdvDialogueChange.AdvancePrompt);
        }

        public bool IsCurrent(in VersionToken version)
        {
            return HasDialogue && version == Version;
        }

        public void Clear()
        {
            _changed.Clear();
            HasDialogue = false;
            TextKey = string.Empty;
            CharacterNameKey = string.Empty;
            Text = string.Empty;
            CharacterName = string.Empty;
            ResetProgress();
        }

        private void ResetProgress()
        {
            _elapsedMs = 0;
            _durationMs = 0;
            _characterCount = 0;
            _measurementReady = false;
            _textCompleted = false;
            _completed = false;
            VisibleCharacterCount = 0;
            AdvancePromptVisible = false;
        }
    }
}
