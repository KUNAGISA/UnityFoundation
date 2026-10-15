using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Common;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    public partial class BlackMaskView : UIView<BlackMaskView.Data>
    {
        public class Data : UIViewData
        {
            public override string Location => "Views/" + nameof(BlackMaskView);
            public override UILayerId LayerId => UILayer.Loading.ToLayerId();
            public override UIViewOptionMask Options => (UIViewOptionFlags.HideMainCamera | UIViewOptionFlags.HideHUD).ToOptionMask();
        }

        [SerializeField] 
        private CanvasGroup _canvasGroup = null;

        protected override void OnHide()
        {
            _canvasGroup.DOKill();
        }

        protected override async UniTask OnPlayShowAnimation(VersionToken _)
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            await _canvasGroup.DOFade(1f, 0.2f);
        }

        protected override async UniTask OnPlayHideAnimation(VersionToken _)
        {
            await _canvasGroup.DOFade(0f, 0.2f);
        }
    }
}
