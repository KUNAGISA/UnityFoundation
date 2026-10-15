using System;
using Gameplay.Sequence;
using Gameplay.UI;

namespace Gameplay
{
    public partial class AdvChoiceItem : UIWidget, IGameplayController
    {
	    internal UnityEngine.UI.Selectable Selectable => _btnChoice;
	    private AdvChoiceToken _token;
	    private int _index = 0;
	    private Action<AdvChoiceToken, int> _onClick = null;
	    
	    internal void SetChoice(AdvChoiceToken token, int index, string text, Action<AdvChoiceToken, int> onClick)
	    {
		    _token = token;
		    _index = index;
		    _txtDesc.text = text ?? string.Empty;
		    _onClick = onClick;
	    }
	    
	    partial void OnChoiceClick()
	    {
		    _onClick?.Invoke(_token, _index);
	    }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class AdvChoiceItem
{
	private void Awake()
	{
		InitializeAllWidgets();
	}
	partial void OnChoiceClick();

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnChoice = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI _txtDesc = null;

	[UnityEngine.ContextMenu("Rebind Widgets"), System.Obsolete("Don't Call In Code.", true)]
	private void RebindAllWidgets()
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(this, "Bind All Widgets");
#endif

		_btnChoice = transform.Find("Choice").GetComponent<UnityEngine.UI.Button>();
		_txtDesc = transform.Find("Choice/Desc").GetComponent<TMPro.TextMeshProUGUI>();

#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}

	private void InitializeAllWidgets()
	{
		_btnChoice.onClick.AddListener(OnChoiceClick);
	}
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
