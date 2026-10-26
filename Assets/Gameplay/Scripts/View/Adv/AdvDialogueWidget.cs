using System;
using Aoiro;
using Gameplay.Common;
using Gameplay.Sequence;
using Gameplay.UI;
using UnityEngine.UI;

namespace Gameplay
{
    public partial class AdvDialogueWidget : UIWidget
    {
        private AdvDialogueSession _session;
        private Action _requestAdvance;
        private SubscriptionToken _changedToken;
        private VersionToken _renderedDialogueVersion;
        private VersionToken _renderedDialogueContentRevision;
        private bool _hasRenderedDialogue;

        public Selectable DefaultSelectable => _btnAdvance;

        public void Bind(AdvDialogueSession session, Action requestAdvance)
        {
            Unbind();
            _session = session;
            _requestAdvance = requestAdvance;
            _changedToken = session.Changed.Subscribe(RefreshDialogue);
            RefreshDialogue(AdvDialogueChangeFlags.All);
        }

        public void Unbind()
        {
            _changedToken.Cancel();
            _changedToken = default;
            _session = null;
            _requestAdvance = null;
            _hasRenderedDialogue = false;
            SetAdvanceEnabled(true);
        }

        public void SetAdvanceEnabled(bool enabled)
        {
            _btnAdvance.enabled = enabled;
        }

        partial void OnAdvanceClick()
        {
            _requestAdvance?.Invoke();
        }

        private void RefreshDialogueText()
        {
            var version = _session.Version;
            var contentRevision = _session.ContentRevision;
            if (!_hasRenderedDialogue || _renderedDialogueVersion != version || _renderedDialogueContentRevision != contentRevision)
            {
                _txtDialogue.text = _session.Text;
                _txtDialogue.ForceMeshUpdate();
                _renderedDialogueVersion = version;
                _renderedDialogueContentRevision = contentRevision;
                _hasRenderedDialogue = true;
                _session.ReportCharacterCount(version, contentRevision, _txtDialogue.textInfo.characterCount);
            }
            _txtDialogue.maxVisibleCharacters = _session.VisibleCharacterCount;
        }

        private void RefreshCharacterName()
        {
            _goCharacterNameBg.SetActive(!string.IsNullOrEmpty(_session.CharacterName));
            _txtCharacterName.text = _session.CharacterName;
        }

        private void RefreshAdvancePrompt()
        {
            _goAdvanceable.SetActive(_session.AdvancePromptVisible);
        }

        private void RefreshDialogue(AdvDialogueChangeFlags change)
        {
            if ((change & AdvDialogueChangeFlags.Text) != 0)
            {
                RefreshDialogueText();
            }
            if ((change & AdvDialogueChangeFlags.CharacterName) != 0)
            {
                RefreshCharacterName();
            }
            if ((change & AdvDialogueChangeFlags.AdvancePrompt) != 0)
            {
                RefreshAdvancePrompt();
            }
        }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class AdvDialogueWidget
{
	private void Awake()
	{
		InitializeAllWidgets();
	}
	partial void OnAdvanceClick();

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnAdvance = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.GameObject _goAdvanceable = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.GameObject _goCharacterNameBg = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI _txtCharacterName = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI _txtDialogue = null;

	[UnityEngine.ContextMenu("Rebind Widgets"), System.Obsolete("Don't Call In Code.", true)]
	private void RebindAllWidgets()
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(this, "Bind All Widgets");
#endif

		_btnAdvance = transform.Find("Advance").GetComponent<UnityEngine.UI.Button>();
		_goAdvanceable = transform.Find("Advance/Advanceable").gameObject;
		_goCharacterNameBg = transform.Find("CharacterNameBg").gameObject;
		_txtCharacterName = transform.Find("CharacterNameBg/CharacterName").GetComponent<TMPro.TextMeshProUGUI>();
		_txtDialogue = transform.Find("Dialogue").GetComponent<TMPro.TextMeshProUGUI>();

#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}

	private void InitializeAllWidgets()
	{
		_btnAdvance.onClick.AddListener(() => OnAdvanceClick());
	}
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
