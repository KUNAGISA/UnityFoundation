using System;
using Aoiro;
using Aoiro.Sequence;
using Gameplay.Common;
using Gameplay.Tables;

namespace Gameplay.Sequence
{
    internal sealed class DialogueAction : SequenceAction<ISequenceContext>, ISequenceAdvanceable
    {
        private readonly string _textKey;
        private readonly string _nameKey;
        private readonly bool _autoAdvanceable;
        private double _elapsedMs;
        private double _durationMs;
        private int _characterCount;
        private bool _advanceable;
        private bool _dialogueCompleted;
        private bool _measurementReady;
        private VersionToken _dialogueVersion;
        private VersionToken _dialogueContentRevision;
        private SequenceSettingsTable _settings;

        internal DialogueAction(string textKey, string nameKey = null, int? audioId = null, bool autoAdvanceable = false)
        {
            _textKey = textKey;
            _nameKey = nameKey;
            _autoAdvanceable = autoAdvanceable;
        }

        protected override bool EvaluateCompletion()
        {
            // 新对白会使旧 Action 失效，旧 Action 应直接完成。
            return _dialogueVersion != Player.GetModule<AdvSession>().DialogueVersion || _advanceable;
        }

        protected override void OnStart()
        {
            ResetDialogueProgress();

            var session = Player.GetModule<AdvSession>();
            _settings = Player.Context.GetModel<TablesModel>().SequenceSettingsTable;
            _dialogueVersion = session.SetDialogue(_textKey, _nameKey);
            _dialogueContentRevision = session.DialogueContentRevision;
        }

        protected override void OnTick(float deltaTime)
        {
            var session = Player.GetModule<AdvSession>();
            if (_dialogueContentRevision != session.DialogueContentRevision)
            {
                _dialogueContentRevision = session.DialogueContentRevision;
                ResetDialogueProgress();
            }

            if (!_measurementReady)
            {
                if (!session.TryGetDialogueCharacterCount(_dialogueVersion, out _characterCount))
                {
                    return;
                }

                _durationMs = _characterCount * _settings.MessageCharIntervalMs + _settings.MessageAdvanceableWaitTimeMs;
                _measurementReady = true;
            }

            _elapsedMs += deltaTime * 1000f;
            if (!_dialogueCompleted)
            {
                UpdateDialogue(session);
            }

            if (_elapsedMs >= _durationMs && _autoAdvanceable)
            {
                _advanceable = true;
            }
        }

        void ISequenceAdvanceable.Advance()
        {
            if (!_measurementReady)
            {
                return;
            }

            // 文本完整显示后的下一次推进才结束 Action。
            _advanceable = _elapsedMs >= _durationMs;
            _elapsedMs = _durationMs;
            _dialogueCompleted = true;

            var session = Player.GetModule<AdvSession>();
            session.SetDialogueVisibleCount(_dialogueVersion, _characterCount);
            session.CompleteDialogue(_dialogueVersion);
        }

        private void UpdateDialogue(AdvSession session)
        {
            var visibleCount = (int)Math.Floor(_elapsedMs / _settings.MessageCharIntervalMs);
            session.SetDialogueVisibleCount(_dialogueVersion, visibleCount);
            if (visibleCount < _characterCount)
            {
                return;
            }

            session.CompleteDialogue(_dialogueVersion);
            _dialogueCompleted = true;
        }

        private void ResetDialogueProgress()
        {
            _elapsedMs = 0;
            _durationMs = 0;
            _characterCount = 0;
            _advanceable = false;
            _dialogueCompleted = false;
            _measurementReady = false;
        }
    }
}
