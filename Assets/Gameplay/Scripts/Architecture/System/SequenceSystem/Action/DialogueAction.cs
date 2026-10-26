using Aoiro.Sequence;
using Gameplay.Common;

namespace Gameplay.Sequence
{
    internal sealed class DialogueAction : SequenceAction<ISequenceContext>, ISequenceAdvanceable
    {
        private readonly string _textKey;
        private readonly string _nameKey;
        private readonly bool _autoAdvance;
        private AdvDialogueSession _dialogue;
        private VersionToken _dialogueVersion;

        internal DialogueAction(string textKey, string nameKey = null, int? audioId = null, bool autoAdvance = false)
        {
            _textKey = textKey;
            _nameKey = nameKey;
            _autoAdvance = autoAdvance;
        }

        protected override void OnStart()
        {
            _dialogue = Player.GetModule<AdvDialogueSession>();
            _dialogueVersion = _dialogue.Begin(_textKey, _nameKey, _autoAdvance);
        }

        protected override bool EvaluateCompletion() => !_dialogue.IsCurrent(_dialogueVersion) || _dialogue.IsCompleted;
        protected override void OnTick(float deltaTime) => _dialogue.Tick(_dialogueVersion, deltaTime);
        void ISequenceAdvanceable.Advance() => _dialogue.Advance(_dialogueVersion);

        protected override void OnCleanup()
        {
            _dialogue = null;
        }
    }
}
