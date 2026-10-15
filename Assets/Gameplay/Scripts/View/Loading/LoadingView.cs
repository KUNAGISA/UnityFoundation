using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay.Common;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    public partial class LoadingView : UIView<LoadingView.Data>
    {
        public class Data : UIViewData
        {
            public override string Location => "Views/" + nameof(LoadingView);
            public override UILayerId LayerId => UILayer.Loading.ToLayerId();
            public override UIViewOptionMask Options => (UIViewOptionFlags.HideMainCamera | UIViewOptionFlags.HideHUD).ToOptionMask();
        }

        [SerializeField] 
        private CanvasGroup _canvasGroup = null;

        protected override void OnShow()
        {
            _txtLoading.DOKill();
            _txtLoading.text = string.Empty;
            _txtLoading.DOText("Loading...", 0.5f, false).SetLoops(-1, LoopType.Restart);
        }

        protected override void OnHide()
        {
            KillTransitions();
        }

        protected override UniTask OnPlayShowAnimation(VersionToken transitionVersion)
        {
            _canvasGroup.DOKill();
            _canvasGroup.alpha = 0f;
            return _canvasGroup.DOFade(1f, 0.2f).ToUniTask();
        }

        protected override UniTask OnPlayHideAnimation(VersionToken _)
        {
            return _canvasGroup.DOFade(0f, 0.2f).ToUniTask();
        }

        private void KillTransitions()
        {
            _canvasGroup.DOKill();
            _txtLoading.DOKill();
        }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class LoadingView
{

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI _txtLoading = null;

	[UnityEngine.ContextMenu("Rebind Widgets"), System.Obsolete("Don't Call In Code.", true)]
	private void RebindAllWidgets()
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(this, "Bind All Widgets");
#endif

		_txtLoading = transform.Find("Loading").GetComponent<TMPro.TextMeshProUGUI>();

#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
