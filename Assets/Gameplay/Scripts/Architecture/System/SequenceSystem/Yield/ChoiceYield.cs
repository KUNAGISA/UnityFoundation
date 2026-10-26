using Aoiro.Sequence;
using Gameplay.Common;

namespace Gameplay.Sequence
{
    internal sealed class ChoiceYield : SequenceYield<ISequenceContext>
    {
        private readonly int _choiceId;
        private readonly string[] _choices;
        private AdvChoiceToken _choiceToken;

        internal ChoiceYield(int choiceId, string[] choices)
        {
            _choiceId = choiceId;
            _choices = choices;
        }

        protected override void OnBegin()
        {
            _choiceToken = Player.GetModule<AdvChoiceSession>().Show(_choiceId, _choices);
        }

        protected override void OnCleanup()
        {
            Player.GetModule<AdvChoiceSession>().Hide(_choiceToken);
        }

        protected override bool EvaluateCompletion()
        {
            return Player.GetModule<AdvChoiceSession>().TryGetSelectedIndex(_choiceToken, out _);
        }

        protected override int? CreateResult()
        {
            return Player.GetModule<AdvChoiceSession>().TryGetSelectedIndex(_choiceToken, out var index) ? index + 1 : 1;
        }
    }
}
