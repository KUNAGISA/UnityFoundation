using System.Collections.Generic;
using Aoiro;
using Gameplay.Asset;
using Gameplay.Common;
using Gameplay.Sequence;
using Gameplay.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public partial class AdvView : UIView<AdvView.Data>, IGameplayController
    {
        public class Data : UIViewData
        {
            public AdvSession Session { get; }

            public Data(AdvSession session) => Session = session;

            public override string Location => "Views/" + nameof(AdvView);
            public override UILayerId LayerId => UILayer.Adv.ToLayerId();
        }

        [SerializeField, AssetsOnly, LabelText("立绘槽预制体"), FoldoutGroup("Binding Prefab")]
        private AdvPortraitSlot _portraitSlotPrefab = null;

        [SerializeField, AssetsOnly, LabelText("选项预制体"), FoldoutGroup("Binding Prefab")]
        private AdvChoiceItem _choiceItemPrefab = null;

        private LocalizationSystem _localizationSystem = null;
        private AdvSession _session = null;
        private AssetLoader _loader = null;
        private SubscriptionToken _changedToken = default;
        private SubscriptionToken _portraitChangedToken = default;
        private VersionToken _renderedDialogueVersion = default;
        private VersionToken _renderedDialogueContentRevision = default;
        private AdvChoiceToken _renderedChoiceToken = default;
        private bool _hasRenderedDialogue;
        private UIViewNavigation _navigation;
        
        private readonly List<AdvChoiceItem> _choiceItems = new List<AdvChoiceItem>();
        private readonly Dictionary<int, AdvPortraitSlot> _portraits = new Dictionary<int, AdvPortraitSlot>();
        private readonly Dictionary<int, string> _portraitLocations = new Dictionary<int, string>();
        private readonly List<AdvPortraitSlot> _portraitSortBuffer = new List<AdvPortraitSlot>();

        protected override void OnInitialize(Data data)
        {
	        _localizationSystem = this.GetSystem<LocalizationSystem>();
	        _navigation = GetComponent<UIViewNavigation>();
	        _loader = this.GetService<IAssetService>().CreateLoader();
	        _session = data.Session;
	        _session.AttachPresentation();
	        _changedToken = _session.Changed.Subscribe(OnSessionChanged);
	        _portraitChangedToken = _session.PortraitChanged.Subscribe(OnPortraitChanged);
	        RefreshSession(AdvChange.All);
	        RefreshPortraits();
        }

        protected override void OnClose()
        {
	        _changedToken.Cancel();
	        _changedToken = default;
	        _portraitChangedToken.Cancel();
	        _portraitChangedToken = default;
	        _session?.DetachPresentation();
	        _session = null;

	        _choiceItems.Clear();
	        _portraits.Clear();
	        _portraitLocations.Clear();
	        _portraitSortBuffer.Clear();
	        _btnAdvance.enabled = true;
	        
	        _localizationSystem = null;
	        _navigation = null;
	        _loader?.Clear();
	        _loader = null;
        }

        private void RefreshDialogue()
        {
	        var version = _session.DialogueVersion;
	        var contentRevision = _session.DialogueContentRevision;
	        if (!_hasRenderedDialogue || _renderedDialogueVersion != version || _renderedDialogueContentRevision != contentRevision)
	        {
		        _txtDialogue.text = _session.DialogueText;
		        _txtDialogue.ForceMeshUpdate();
		        _renderedDialogueVersion = version;
		        _renderedDialogueContentRevision = contentRevision;
		        _hasRenderedDialogue = true;
		        _session.ReportDialogueMeasurement(version, _txtDialogue.textInfo.characterCount);
	        }
	        _txtDialogue.maxVisibleCharacters = _session.DialogueVisibleCount;
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

        private void RefreshPortraits()
        {
	        foreach (var (portraitId, entry) in _session.Portraits)
	        {
		        RefreshPortraitAsset(portraitId, entry);
	        }
	        SortPortraits();
        }

        private void OnPortraitChanged(AdvPortraitChange change)
        {
	        if (_session == null || !_session.TryGetPortrait(change.PortraitId, out var entry))
	        {
		        return;
	        }

	        switch (change.Type)
	        {
		        case AdvPortraitChangeType.Visual:
			        if (_portraits.TryGetValue(change.PortraitId, out var portrait))
			        {
				        portrait.ApplyState(entry.VisualState);
			        }
			        break;
		        case AdvPortraitChangeType.Asset:
			        var created = !_portraits.ContainsKey(change.PortraitId);
			        RefreshPortraitAsset(change.PortraitId, entry);
			        if (created)
			        {
				        SortPortraits();
			        }
			        break;
		        case AdvPortraitChangeType.SortingOrder:
			        if (_portraits.TryGetValue(change.PortraitId, out var slot))
			        {
				        slot.SetSortingOrder(entry.SortingOrder);
				        SortPortraits();
			        }
			        break;
	        }
        }

        private void RefreshPortraitAsset(int portraitId, AdvPortraitEntry entry)
        {
	        if (!_portraits.TryGetValue(portraitId, out var portraitSlot))
	        {
		        portraitSlot = Instantiate(_portraitSlotPrefab, _tfPortraits, false);
#if UNITY_EDITOR
		        portraitSlot.name = $"Portrait_{portraitId}";
#endif
		        _portraits[portraitId] = portraitSlot;
	        }

	        if (!_portraitLocations.TryGetValue(portraitId, out var assetLocation) || assetLocation != entry.AssetLocation)
	        {
		        _portraitLocations[portraitId] = entry.AssetLocation;
		        var prefab = _loader.LoadAssetSync<GameObject>(entry.AssetLocation);
		        var instance = prefab == null ? null : Instantiate(prefab, Vector3.zero, Quaternion.identity, _tfPortraits);
		        if (instance == null || !instance.TryGetComponent<AdvPortrait>(out var portrait))
		        {
			        portraitSlot.ClearPortrait();
			        Debug.LogError($"[Sequence] {entry.AssetLocation} 实例化失败或者找不到组件 {typeof(AdvPortrait)}");
		        }
		        else
		        {
			        portraitSlot.SetPortrait(portrait);
		        }
	        }

	        portraitSlot.ApplyState(entry.VisualState);
	        portraitSlot.SetSortingOrder(entry.SortingOrder);
        }

        private void RefreshChoices()
        {
	        var choices = _session.Choices;
	        if (!_session.ChoicesVisible || choices == null)
	        {
		        foreach (var choiceItem in _choiceItems)
		        {
			        choiceItem.gameObject.SetActive(false);
		        }
		        _goChoices.SetActive(false);
		        _btnAdvance.enabled = true;
		        _navigation?.RefreshFocus(_btnAdvance);
		        return;
	        }
	        
	        var length = choices.Length;
	        _renderedChoiceToken = _session.ChoiceToken;
	        if (_choiceItems.Capacity < length)
	        {
		        _choiceItems.Capacity = length;
	        }

	        for (var index = _choiceItems.Count; index < length; ++index)
	        {
		        var choiceItem = Instantiate(_choiceItemPrefab, _goChoices.transform, false);
		        _choiceItems.Add(choiceItem);
	        }

	        for (var index = 0; index < length; ++index)
	        {
		        var text = _localizationSystem.Get(choices[index], TextSourceFlags.Global | TextSourceFlags.Sequence);
		        _choiceItems[index].SetChoice(_renderedChoiceToken, index, text, OnChoiceClick);
		        _choiceItems[index].Selectable.navigation = new Navigation()
		        {
			        mode = Navigation.Mode.Explicit,
			        selectOnUp = index > 0 ? _choiceItems[index - 1].Selectable : null,
			        selectOnDown = index < (length - 1) ? _choiceItems[index + 1].Selectable : null,
		        };
	        }
	        _btnAdvance.enabled = length == 0;
	        _goChoices.SetActive(length > 0);
	        if (length > 0)
	        {
		        _navigation?.RefreshFocus(_choiceItems[0].Selectable);
	        }
	        else
	        {
		        _navigation?.RefreshFocus(_btnAdvance);
	        }

	        for (var index = length; index < _choiceItems.Count; ++index)
	        {
		        _choiceItems[index].gameObject.SetActive(false);
	        }
        }

        private void SortPortraits()
        {
	        foreach (var portrait in _portraits.Values)
	        {
		        _portraitSortBuffer.Add(portrait);
	        }

	        _portraitSortBuffer.Sort((a, b) =>
	        {
		        var result = a.SortingOrder.CompareTo(b.SortingOrder);
		        return result != 0 ? result : a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
	        });
	        for (var index = 0; index < _portraitSortBuffer.Count; index++)
	        {
		        _portraitSortBuffer[index].transform.SetSiblingIndex(index);
	        }
	        _portraitSortBuffer.Clear();
        }
        
        private void OnChoiceClick(AdvChoiceToken token, int index)
        {
	        _session?.SelectChoice(token, index);
        }
        
        partial void OnAdvanceClick()
        {
	        _session?.RequestAdvance();
        }

        private void OnSessionChanged(AdvChange change) => RefreshSession(change);

        private void RefreshSession(AdvChange change)
        {
	        if ((change & AdvChange.Dialogue) != 0)
	        {
		        RefreshDialogue();
	        }
	        if ((change & AdvChange.CharacterName) != 0)
	        {
		        RefreshCharacterName();
	        }
	        if ((change & AdvChange.AdvancePrompt) != 0)
	        {
		        RefreshAdvancePrompt();
	        }
	        if ((change & AdvChange.Choices) != 0)
	        {
		        RefreshChoices();
	        }
        }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class AdvView
{
	private void Awake()
	{
		InitializeAllWidgets();
	}
	partial void OnAdvanceClick();

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.GameObject _goChoices = null;
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
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.RectTransform _tfPortraits = null;

	[UnityEngine.ContextMenu("Rebind Widgets"), System.Obsolete("Don't Call In Code.", true)]
	private void RebindAllWidgets()
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(this, "Bind All Widgets");
#endif

		_goChoices = transform.Find("Choices").gameObject;
		_btnAdvance = transform.Find("DialogueBg/Advance").GetComponent<UnityEngine.UI.Button>();
		_goAdvanceable = transform.Find("DialogueBg/Advance/Advanceable").gameObject;
		_goCharacterNameBg = transform.Find("DialogueBg/CharacterNameBg").gameObject;
		_txtCharacterName = transform.Find("DialogueBg/CharacterNameBg/CharacterName").GetComponent<TMPro.TextMeshProUGUI>();
		_txtDialogue = transform.Find("DialogueBg/Dialogue").GetComponent<TMPro.TextMeshProUGUI>();
		_tfPortraits = transform.Find("Portraits") as UnityEngine.RectTransform;

#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}

	private void InitializeAllWidgets()
	{
		_btnAdvance.onClick.AddListener(OnAdvanceClick);
	}
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
