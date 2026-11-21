using Aoiro.Sequence;

namespace Gameplay.Sequence
{
    internal sealed class WaitActionYield : SequenceYield<ISequenceContext>
    {
        protected override bool EvaluateCompletion()
        {
            return Player.TotalActionCount == 0;
        }
    }
}
