using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    public sealed partial class SaveLoadView : UIView<SaveLoadView.Data>, IGameplayController, IUIInputCommandTarget
    {
        private const int SlotCount = 3;

        public enum Mode : byte
        {
            Save,
            Load,
        }

        public sealed class Data : UIViewData
        {
            public Mode ViewMode { get; }
            public override string Location => "Views/" + nameof(SaveLoadView);
            public override UILayerId LayerId => UILayer.Modal.ToLayerId();
            public override UIViewOptionMask Options => (UIViewOptionFlags.Blur | UIViewOptionFlags.BlockGameplayInput).ToOptionMask();

            public Data(Mode mode)
            {
                ViewMode = mode;
            }
        }

        private readonly SaveSlotSummary[] _slotSummaries = new SaveSlotSummary[SlotCount];
        private CancellationTokenSource _lifetimeCancellation;
        private Mode _mode;
        private bool _isBusy;
        private UIViewNavigation _navigation;

        protected override void OnInitialize(Data data)
        {
            _mode = data.ViewMode;
            _navigation = GetComponent<UIViewNavigation>();
            _lifetimeCancellation = new CancellationTokenSource();
            _txtTitle.text = _mode == Mode.Save ? "保存游戏" : "读取游戏";
            _txtResult.text = string.Empty;
            SetBusy(false, null);
            try
            {
                RefreshSlotSummaries();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _txtResult.text = $"读取槽位失败：{exception.Message}";
            }
        }

        protected override void OnClose()
        {
            _lifetimeCancellation?.Cancel();
            _lifetimeCancellation?.Dispose();
            _lifetimeCancellation = null;
            _navigation = null;
        }

        private async UniTask RunSlotOperationAsync(int slot)
        {
            try
            {
                var cancellationToken = _lifetimeCancellation.Token;
                SetBusy(true, _mode == Mode.Save ? "正在保存..." : "正在读取...");
                if (_mode == Mode.Save)
                {
                    await SaveSlotAsync(slot, cancellationToken);
                }
                else
                {
                    await LoadSlotAsync(slot, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (_lifetimeCancellation == null || _lifetimeCancellation.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _txtResult.text = $"操作失败：{exception.Message}";
                SetBusy(false, null);
            }
        }

        private async UniTask SaveSlotAsync(int slot, CancellationToken cancellationToken)
        {
            var result = await this.GetSystem<SaveSystem>().SaveAsync(slot, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Status == SaveOperationStatus.Succeeded)
            {
                RefreshSlotSummaries();
                _txtResult.text = $"槽位 {slot + 1} 保存成功。";
            }
            else
            {
                _txtResult.text = result.Status == SaveOperationStatus.Canceled ? "保存已取消。" : $"保存失败：{result.Error}";
            }
            SetBusy(false, null);
        }

        private async UniTask LoadSlotAsync(int slot, CancellationToken cancellationToken)
        {
            var result = await this.GetSystem<SaveSystem>().LoadAsync(slot, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (result.Status != SaveOperationStatus.Succeeded)
            {
                _txtResult.text = result.Status == SaveOperationStatus.Canceled ? "读取已取消。" : $"读取失败：{result.Error}";
                SetBusy(false, null);
                return;
            }

            _txtResult.text = "读取成功，正在进入游戏...";
            this.ExecuteCommand(new EnterGameCommand(result.SaveData));
            CloseSelf(true);
        }

        private void RefreshSlotSummaries()
        {
            var saveSystem = this.GetSystem<SaveSystem>();
            for (var slot = 0; slot < SlotCount; slot++)
            {
                _slotSummaries[slot] = saveSystem.GetSlotSummary(slot);
                RefreshSlot(slot);
            }
        }

        private void RefreshSlot(int slot)
        {
            var summary = _slotSummaries[slot];
            var slotText = _txtSlotText[slot];
            switch (summary.State)
            {
                case SaveSlotState.Empty:
                    slotText.text = $"槽位 {slot + 1}\n空槽位";
                    break;
                case SaveSlotState.Available:
                    var localTime = new DateTime(summary.SaveTimeUtcTicks, DateTimeKind.Utc).ToLocalTime();
                    slotText.text = $"槽位 {slot + 1}\n{localTime:yyyy-MM-dd HH:mm:ss}\n场景 {summary.SceneId}";
                    break;
                default:
                    slotText.text = $"槽位 {slot + 1}\n存档损坏";
                    break;
            }

            _btnSlot[slot].interactable = !_isBusy && (_mode == Mode.Save || summary.State == SaveSlotState.Available);
        }

        private void SetBusy(bool busy, string message)
        {
            _isBusy = busy;
            _btnClose.interactable = !busy;
            _goBusyBlocker.SetActive(busy);
            _txtBusyText.text = message ?? string.Empty;
            for (var slot = 0; slot < SlotCount; slot++)
            {
                RefreshSlot(slot);
            }
        }

        public bool TryConsumeUIInputCommand(UIInputCommand command)
        {
            if (command != UIInputCommand.Cancel)
            {
                return false;
            }
            if (!_isBusy)
            {
                CloseSelf();
            }
            return true;
        }

        partial void OnCloseClick()
        {
            if (!_isBusy)
            {
                CloseSelf();
            }
        }

        partial void OnSlotClick(int index)
        {
            if (_isBusy)
            {
                return;
            }
            RunSlotOperationAsync(index).Forget();
        }
    }
}

#region Auto Generate UI Bind
#pragma warning disable IDE0052, IDE0001
namespace Gameplay
{
partial class SaveLoadView
{
	private void Awake()
	{
		InitializeAllWidgets();
	}
	partial void OnCloseClick();
	partial void OnSlotClick(int index);

	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.GameObject _goBusyBlocker = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI _txtBusyText = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI _txtResult = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button _btnClose = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI _txtTitle = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private UnityEngine.UI.Button[] _btnSlot = null;
	[UnityEngine.SerializeField, Sirenix.OdinInspector.FoldoutGroup("Binding Weight")]
	private TMPro.TextMeshProUGUI[] _txtSlotText = null;

	[UnityEngine.ContextMenu("Rebind Widgets"), System.Obsolete("Don't Call In Code.", true)]
	private void RebindAllWidgets()
	{
#if UNITY_EDITOR
		UnityEditor.Undo.RecordObject(this, "Bind All Widgets");
#endif

		_goBusyBlocker = transform.Find("BusyBlocker").gameObject;
		_txtBusyText = transform.Find("BusyBlocker/BusyText").GetComponent<TMPro.TextMeshProUGUI>();
		_txtResult = transform.Find("Panel/Result").GetComponent<TMPro.TextMeshProUGUI>();
		_btnClose = transform.Find("Panel/Close").GetComponent<UnityEngine.UI.Button>();
		_txtTitle = transform.Find("Panel/Title").GetComponent<TMPro.TextMeshProUGUI>();
		_btnSlot = new UnityEngine.UI.Button[3];
		_btnSlot[0] = transform.Find("Panel/Slots").GetChild(0).GetComponent<UnityEngine.UI.Button>();
		_btnSlot[1] = transform.Find("Panel/Slots").GetChild(1).GetComponent<UnityEngine.UI.Button>();
		_btnSlot[2] = transform.Find("Panel/Slots").GetChild(2).GetComponent<UnityEngine.UI.Button>();
		_txtSlotText = new TMPro.TextMeshProUGUI[3];
		_txtSlotText[0] = transform.Find("Panel/Slots").GetChild(0).Find("SlotText").GetComponent<TMPro.TextMeshProUGUI>();
		_txtSlotText[1] = transform.Find("Panel/Slots").GetChild(1).Find("SlotText").GetComponent<TMPro.TextMeshProUGUI>();
		_txtSlotText[2] = transform.Find("Panel/Slots").GetChild(2).Find("SlotText").GetComponent<TMPro.TextMeshProUGUI>();

#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
#endif
	}

	private void InitializeAllWidgets()
	{
		_btnClose.onClick.AddListener(() => OnCloseClick());
		_btnSlot[0].onClick.AddListener(() => OnSlotClick(0));
		_btnSlot[1].onClick.AddListener(() => OnSlotClick(1));
		_btnSlot[2].onClick.AddListener(() => OnSlotClick(2));
	}
}
}
#pragma warning restore IDE0052, IDE0001
#endregion Auto Generate UI Bind
