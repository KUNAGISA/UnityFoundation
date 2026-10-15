using Aoiro.Sequence;

namespace Gameplay.Sequence
{
    internal sealed class WaitTimeYield : SequenceYield<ISequenceContext>, ISequenceAdvanceable
    {
        private double _time = 0f;

        internal WaitTimeYield(double time)
        {
            _time = time;
        }
        
        protected override bool EvaluateCompletion()
        {
            return _time <= 0d;
        }

        protected override void OnTick(float deltaTime)
        {
            _time -= deltaTime;
        }

        void ISequenceAdvanceable.Advance()
        {
            _time = 0f;
        }
    }
}
