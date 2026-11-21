using Aoiro;
using FancyScrollView.Universal;
using Gameplay.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public sealed class InputNavigationTestView : UIView<InputNavigationTestView.Data>, IGameplayController, IUIInputCommandTarget
    {
        public sealed class Data : UIViewData
        {
            public override string Location => "Views/" + nameof(InputNavigationTestView);
            public override UILayerId LayerId => UILayer.Overlay.ToLayerId();
            public override UIViewOptionMask Options => (UIViewOptionFlags.Blur | UIViewOptionFlags.BlockGameplayInput).ToOptionMask();
        }

        private static readonly string[] ScrollItems = { "Scroll 0", "Scroll 1", "Scroll 2（禁用）", "Scroll 3", "Scroll 4", "Scroll 5", "Scroll 6", "Scroll 7" };
        private static readonly string[] GridItems = { "Grid 0", "Grid 1", "Grid 2（禁用）", "Grid 3", "Grid 4", "Grid 5", "Grid 6", "Grid 7", "Grid 8", "Grid 9", "Grid 10", "Grid 11" };

        [SerializeField] private UIViewNavigation _navigation = null;
        [SerializeField] private GameObject _childPanel = null;
        [SerializeField] private GameObject _scrollPanel = null;
        [SerializeField] private GameObject _gridPanel = null;
        [SerializeField] private TMP_Text _status = null;
        [SerializeField] private Button _openChild = null;
        [SerializeField] private Button _openScroll = null;
        [SerializeField] private Button _openGrid = null;
        [SerializeField] private Button _openLowerLayer = null;
        [SerializeField] private Button _close = null;
        [SerializeField] private Button _childAction = null;
        [SerializeField] private Button _childBack = null;
        [SerializeField] private UniversalFancyScrollRect _scrollRect = null;
        [SerializeField] private Button _scrollBack = null;
        [SerializeField] private UniversalFancyGridView _gridView = null;
        [SerializeField] private Button _gridBack = null;

        private UIFocusScopeToken _currentFocusScopeToken;
        private UIViewToken _lowerLayerToken;
        private bool _fancyInitialized;

        protected override void OnInitialize(Data data)
        {
            _openChild.onClick.AddListener(OpenChild);
            _openScroll.onClick.AddListener(OpenScroll);
            _openGrid.onClick.AddListener(OpenGrid);
            _openLowerLayer.onClick.AddListener(OpenLowerLayer);
            _close.onClick.AddListener(CloseView);
            _childAction.onClick.AddListener(() => SetStatus("子节点 Submit"));
            _childBack.onClick.AddListener(CloseCurrentFocusScope);
            _scrollBack.onClick.AddListener(CloseCurrentFocusScope);
            _gridBack.onClick.AddListener(CloseCurrentFocusScope);
            _scrollRect.SetItemActivation(OnScrollItemActivated);
            _gridView.SetItemActivation(OnGridItemActivated);

            SetStatus("根焦点作用域：Y / ContextNorth 可测试路由");
        }

        protected override void OnShow()
        {
            if (_fancyInitialized)
            {
                return;
            }
            _scrollRect.SharedContext.Set<ISelection>(new SingleSelection((_, index) => index != 2));
            _scrollRect.SetItems(ScrollItems);
            _gridView.SharedContext.Set<ISelection>(new SingleSelection((_, index) => index != 2));
            _gridView.SetItems(GridItems);
            _childPanel.SetActive(false);
            _scrollPanel.SetActive(false);
            _gridPanel.SetActive(false);
            _fancyInitialized = true;
        }

        protected override void OnClose()
        {
            _lowerLayerToken.Close(true);
            _lowerLayerToken = default;
            _openChild.onClick.RemoveListener(OpenChild);
            _openScroll.onClick.RemoveListener(OpenScroll);
            _openGrid.onClick.RemoveListener(OpenGrid);
            _openLowerLayer.onClick.RemoveListener(OpenLowerLayer);
            _close.onClick.RemoveListener(CloseView);
            _childBack.onClick.RemoveListener(CloseCurrentFocusScope);
            _scrollBack.onClick.RemoveListener(CloseCurrentFocusScope);
            _gridBack.onClick.RemoveListener(CloseCurrentFocusScope);
        }

        public bool TryConsumeUIInputCommand(UIInputCommand command)
        {
            if (command == UIInputCommand.Cancel)
            {
                if (_navigation.IsCurrentFocusScope(gameObject))
                {
                    CloseView();
                }
                else
                {
                    CloseCurrentFocusScope();
                }
                return true;
            }
            
            if (command != UIInputCommand.ContextNorth)
            {
                return false;
            }

            var scopeName = _navigation.IsCurrentFocusScope(gameObject) ? "根节点" : _navigation.IsCurrentFocusScope(_childPanel) ? "独占子节点" : _navigation.IsCurrentFocusScope(_scrollPanel) ? "Fancy Scroll" : "Fancy Grid";
            SetStatus($"{scopeName} 收到 ContextNorth");
            return true;
        }

        private void OpenChild()
        {
            CloseCurrentFocusScope();
            _childPanel.SetActive(true);
            _currentFocusScopeToken = _navigation.PushFocusScope(_childPanel);
        }

        private void OpenScroll()
        {
            CloseCurrentFocusScope();
            _scrollPanel.SetActive(true);
            _currentFocusScopeToken = _navigation.PushFocusScope(_scrollPanel);
        }

        private void OpenGrid()
        {
            CloseCurrentFocusScope();
            _gridPanel.SetActive(true);
            _currentFocusScopeToken = _navigation.PushFocusScope(_gridPanel);
        }

        private void OpenLowerLayer()
        {
            _lowerLayerToken.Close(true);
            _lowerLayerToken = this.GetSystem<UISystem>().OpenView(new SaveLoadView.Data(SaveLoadView.Mode.Load));
            SetStatus("已后开 Modal 层 SaveLoad；Overlay 应继续持有焦点");
        }

        private void CloseCurrentFocusScope()
        {
            _currentFocusScopeToken.Cancel();
            _currentFocusScopeToken = default;
            
            _childPanel.SetActive(false);
            _scrollPanel.SetActive(false);
            _gridPanel.SetActive(false);
        }

        private void CloseView() => CloseSelf();
        private void OnScrollItemActivated(int index) => SetStatus($"Scroll Activate：{index}");
        private void OnGridItemActivated(int index) => SetStatus($"Grid Activate：{index}");
        private void SetStatus(string message) => _status.text = message;
    }
}
