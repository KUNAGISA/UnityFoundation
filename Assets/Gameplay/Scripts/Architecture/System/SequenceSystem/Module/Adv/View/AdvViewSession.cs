using System;
using Aoiro;
using Aoiro.Sequence;
using Gameplay.UI;

namespace Gameplay.Sequence
{
    public sealed class AdvViewSession : SequenceModule<ISequenceContext>
    {
        private enum ViewState
        {
            None,
            Opening,
            Bound,
            Closed,
            Cleaning
        }

        private UIViewToken _viewToken = UIViewToken.None;
        private ViewState _viewState;
        private bool _advanceRequested;

        public AdvDialogueSession Dialogue { get; private set; }
        public AdvChoiceSession Choices { get; private set; }
        public AdvPortraitSession Portraits { get; private set; }
        public bool IsViewBound => _viewState == ViewState.Bound;

        protected override SequencePreparation OnPrepare()
        {
            Dialogue = Player.GetModule<AdvDialogueSession>();
            Choices = Player.GetModule<AdvChoiceSession>();
            Portraits = Player.GetModule<AdvPortraitSession>();
            _advanceRequested = false;
            _viewState = ViewState.Opening;
            _viewToken = Player.Context.GetSystem<UISystem>().OpenView(new AdvView.Data(this));
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
            var viewToken = _viewToken;
            _viewToken = UIViewToken.None;
            viewToken.Cancel();
            Dialogue = null;
            Choices = null;
            Portraits = null;
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
    }
}
