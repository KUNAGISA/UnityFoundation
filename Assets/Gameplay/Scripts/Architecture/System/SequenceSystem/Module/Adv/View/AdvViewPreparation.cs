using System;
using Aoiro.Sequence;
using Gameplay.UI;

namespace Gameplay.Sequence
{
    internal sealed class AdvViewPreparation : SequencePreparation
    {
        private readonly AdvViewSession _session;
        private readonly UIViewToken _token;

        internal AdvViewPreparation(AdvViewSession session, UIViewToken token)
        {
            _session = session;
            _token = token;
        }

        protected override bool EvaluateCompletion()
        {
            var lifecycle = _token.GetStatus().Lifecycle;
            if (lifecycle == UIViewLifecycle.Loading)
            {
                return false;
            }

            if (lifecycle != UIViewLifecycle.Opened)
            {
                throw new InvalidOperationException($"AdvView [{_token.ViewId}] did not open: {lifecycle}.");
            }

            if (!_session.IsViewBound)
            {
                throw new InvalidOperationException($"AdvView [{_token.ViewId}] did not bind its session during initialization.");
            }

            return true;
        }
    }
}
