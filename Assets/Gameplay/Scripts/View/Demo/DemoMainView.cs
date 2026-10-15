using Aoiro;
using Gameplay.UI;

namespace Gameplay
{
    public partial class DemoMainView : UIView<DemoMainView.Data>, IGameplayController
    {
        public class Data : UIViewData
        {
            public override string Location => "Views/" + nameof(DemoMainView);
            public override UILayerId LayerId => UILayer.HUD.ToLayerId();
        }

        partial void OnBtnReturnClick()
        {
	        this.GetSystem<ProcedureSystem>().Transition(new TransitionProcedureRequest(ProcedureType.MainMenu)
	        {
		        Reason = TransitionProcedureReason.Quit,
	        });
        }

        partial void OnBtnSaveClick()
        {
            this.GetSystem<UISystem>().OpenView(new SaveLoadView.Data(SaveLoadView.Mode.Save));
        }

        partial void OnBtnLoadClick()
        {
            this.GetSystem<UISystem>().OpenView(new SaveLoadView.Data(SaveLoadView.Mode.Load));
        }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class DemoMainView
{
	private void Awake()
	{
		InitializeAllWidgets();
	}
	partial void OnBtnReturnClick();
	partial void OnBtnSaveClick();
	partial void OnBtnLoadClick();

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnReturn = null;

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnSave = null;

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnLoad = null;

	[UnityEngine.ContextMenu("Rebind Widgets"), System.Obsolete("Don't Call In Code.", true)]
	private void RebindAllWidgets()
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(this, "Bind All Widgets");
#endif

		_btnReturn = transform.Find("BtnReturn").GetComponent<UnityEngine.UI.Button>();
		_btnSave = transform.Find("BtnSave").GetComponent<UnityEngine.UI.Button>();
		_btnLoad = transform.Find("BtnLoad").GetComponent<UnityEngine.UI.Button>();

#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}

	private void InitializeAllWidgets()
	{
		_btnReturn.onClick.AddListener(OnBtnReturnClick);
		_btnSave.onClick.AddListener(OnBtnSaveClick);
		_btnLoad.onClick.AddListener(OnBtnLoadClick);
	}
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
