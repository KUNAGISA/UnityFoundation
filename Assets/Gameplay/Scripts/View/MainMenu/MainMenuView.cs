using Aoiro;
using Gameplay.UI;

namespace Gameplay
{
    public partial class MainMenuView : UIView<MainMenuView.Data>, IGameplayController
    {
        public class Data : UIViewData
        {
	        public override string Location => "Views/" + nameof(MainMenuView);
            public override bool HideViewsBelow => true;
            public override UIViewOptionMask Options => (UIViewOptionFlags.HideMainCamera | UIViewOptionFlags.HideHUD).ToOptionMask();
        }

        partial void OnStartGameClick()
        {
	        this.ExecuteCommand(new EnterGameCommand(reason: TransitionProcedureReason.Initialize));
        }

        partial void OnLoadGameClick()
        {
            this.GetSystem<UISystem>().OpenView(new SaveLoadView.Data(SaveLoadView.Mode.Load));
        }

        partial void OnInputNavigationTestClick()
        {
            this.GetSystem<UISystem>().OpenView(new InputNavigationTestView.Data());
        }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class MainMenuView
{
	private void Awake()
	{
		InitializeAllWidgets();
	}
	partial void OnStartGameClick();
	partial void OnLoadGameClick();
	partial void OnInputNavigationTestClick();

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnStartGame = null;

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnLoadGame = null;

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnInputNavigationTest = null;

	[UnityEngine.ContextMenu("Rebind Widgets"), System.Obsolete("Don't Call In Code.", true)]
	private void RebindAllWidgets()
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(this, "Bind All Widgets");
#endif

		_btnStartGame = transform.Find("StartGame").GetComponent<UnityEngine.UI.Button>();
		_btnLoadGame = transform.Find("LoadGame").GetComponent<UnityEngine.UI.Button>();
		_btnInputNavigationTest = transform.Find("InputNavigationTest").GetComponent<UnityEngine.UI.Button>();

#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}

	private void InitializeAllWidgets()
	{
		_btnStartGame.onClick.AddListener(OnStartGameClick);
		_btnLoadGame.onClick.AddListener(OnLoadGameClick);
		_btnInputNavigationTest.onClick.AddListener(OnInputNavigationTestClick);
	}
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
