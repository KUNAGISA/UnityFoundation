using Aoiro;
using Gameplay.Asset;
using Gameplay.Sequence;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    public class AdvView : UIView<AdvView.Data>, IGameplayController
    {
        public class Data : UIViewData
        {
            public AdvViewSession Session { get; }

            public Data(AdvViewSession session) => Session = session;

            public override string Location => "Views/" + nameof(AdvView);
            public override UILayerId LayerId => UILayer.Adv.ToLayerId();
        }

        [SerializeField]
        private AdvDialogueWidget _dialogueWidget = null;
        [SerializeField]
        private AdvChoiceWidget _choiceWidget = null;
        [SerializeField]
        private AdvPortraitWidget _portraitWidget = null;

        private AdvViewSession _session;
        private UIViewNavigation _navigation;
        private SubscriptionToken _choicesChangedToken;

        protected override void OnInitialize(Data data)
        {
            BindSession(data.Session);
        }

        protected override void OnRefresh(Data data)
        {
            UnbindSession();
            BindSession(data.Session);
        }

        protected override void OnClose()
        {
            UnbindSession();
        }

        private void BindSession(AdvViewSession session)
        {
            _session = session;
            _navigation = GetComponent<UIViewNavigation>();
            _dialogueWidget.Bind(session.Dialogue, session.RequestAdvance);
            _choiceWidget.Bind(session.Choices);
            _portraitWidget.Bind(session.Portraits, this.GetService<IAssetService>());
            // Widget 先刷新选项，View 再将焦点移到已经可用的目标。
            _choicesChangedToken = session.Choices.Changed.Subscribe(RefreshInput);
            RefreshInput();
            session.NotifyViewBound();
        }

        private void UnbindSession()
        {
            _choicesChangedToken.Cancel();
            _choicesChangedToken = default;
            _portraitWidget.Unbind();
            _choiceWidget.Unbind();
            _dialogueWidget.Unbind();
            _session?.NotifyViewClosed();
            _session = null;
            _navigation = null;
        }

        private void RefreshInput()
        {
            var choiceSelectable = _choiceWidget.DefaultSelectable;
            _dialogueWidget.SetAdvanceEnabled(choiceSelectable == null);
            _navigation?.RefreshFocus(choiceSelectable != null ? choiceSelectable : _dialogueWidget.DefaultSelectable);
        }
    }
}
