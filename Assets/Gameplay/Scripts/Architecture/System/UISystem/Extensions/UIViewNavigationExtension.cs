using System;
using System.Collections.Generic;
using Aoiro;
using Gameplay.UI;

namespace Gameplay
{
    internal sealed class UIViewNavigationExtension : UIViewExtension
    {
        private sealed class ViewRecord
        {
            public UIViewToken Token;
            public int SortingOrder;
            public UIViewNavigation Navigation;
            public bool IsVisible;
            public bool ShowCompleted;
        }

        private readonly Dictionary<UIViewToken, ViewRecord> _records = new();
        private readonly List<ViewRecord> _orderedRecords = new();
        private readonly IInputService _inputService;
        private readonly UIRoot _root;
        private SubscriptionToken _commandToken;
        private SubscriptionToken _presentationChangedToken;
        private InputModeToken _inputModeToken;
        private ViewRecord _activeRecord;

        internal UIViewNavigationExtension(IInputService inputService, UIRoot root)
        {
            _inputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _commandToken = inputService.UICommand.Subscribe(RouteCommand);
            _presentationChangedToken = inputService.InputPresentationChanged.Subscribe(OnInputPresentationChanged);
        }

        protected override void OnBeforeShow(in UIViewExtensionContext context)
        {
            if (!_records.TryGetValue(context.Token, out var record))
            {
                if (!_root.TryGetLayer(context.LayerId, out var layer))
                {
                    throw new InvalidOperationException($"UI layer [{context.LayerId}] is not configured by the UI root.");
                }

                record = new ViewRecord
                {
                    Token = context.Token,
                    SortingOrder = layer.SortingOrder,
                    Navigation = context.View != null ? context.View.GetComponent<UIViewNavigation>() : null
                };
                _records.Add(record.Token, record);
                _orderedRecords.Add(record);
            }

            record.IsVisible = true;
            record.ShowCompleted = false;
            RefreshRoute();
        }

        protected override void OnShowCompleted(in UIViewExtensionContext context)
        {
            if (_records.TryGetValue(context.Token, out var record))
            {
                record.ShowCompleted = true;
                RefreshRoute();
            }
        }

        protected override void OnBeforeHide(in UIViewExtensionContext context)
        {
            if (_records.TryGetValue(context.Token, out var record))
            {
                record.IsVisible = false;
                record.ShowCompleted = false;
                RefreshRoute();
            }
        }

        protected override void OnReleased(in UIViewExtensionContext context)
        {
            if (!_records.Remove(context.Token, out var record))
            {
                return;
            }

            if (ReferenceEquals(_activeRecord, record))
            {
                record.Navigation.SetRouteActive(false);
                _activeRecord = null;
            }
            _orderedRecords.Remove(record);
            record.Navigation?.Release();
            RefreshRoute();
        }

        private void RouteCommand(UIInputCommand command) => _activeRecord?.Navigation.RouteCommand(command);

        protected override void OnShutdown(UIViewHost host)
        {
            _commandToken.Cancel();
            _commandToken = default;
            _presentationChangedToken.Cancel();
            _presentationChangedToken = default;

            if (_activeRecord != null)
            {
                _activeRecord.Navigation.SetRouteActive(false);
                _activeRecord = null;
            }
            for (var index = 0; index < _orderedRecords.Count; index++)
            {
                _orderedRecords[index].Navigation?.Release();
            }
            _records.Clear();
            _orderedRecords.Clear();
            CancelInputMode();
        }

        private void RefreshRoute()
        {
            var top = FindTopRecord();
            var nextActive = top != null && top.ShowCompleted ? top.Navigation : null;
            var nextRecord = nextActive != null ? top : null;
            if (!ReferenceEquals(_activeRecord, nextRecord))
            {
                _activeRecord?.Navigation.SetRouteActive(false);
                _activeRecord = nextRecord;
                _activeRecord?.Navigation.SetRouteActive(true);
                _activeRecord?.Navigation.SetInteractionSource(_inputService.CurrentPresentation.InteractionSource);
            }

            if (top?.Navigation != null)
            {
                if (!_inputModeToken.IsValid)
                {
                    _inputModeToken = _inputService.PushMode(InputModeCatalog.UINavigation);
                }
            }
            else
            {
                CancelInputMode();
            }
        }

        private void OnInputPresentationChanged(InputPresentationChanged change)
        {
            var navigation = _activeRecord?.Navigation;
            if (navigation == null || change.Previous.InteractionSource == change.Current.InteractionSource || !_inputService.TryGetPointerPosition(out var pointerPosition))
            {
                return;
            }

            if (change.Current.InteractionSource == UIInteractionSource.Navigation)
            {
                navigation.TakeNavigationControl(pointerPosition, change.Previous.DeviceKind == InputDeviceKind.KeyboardMouse);
                return;
            }
            navigation.TakePointerControl(pointerPosition, change.Current.DeviceKind == InputDeviceKind.KeyboardMouse);
        }

        private ViewRecord FindTopRecord()
        {
            ViewRecord top = null;
            for (var index = 0; index < _orderedRecords.Count; index++)
            {
                var record = _orderedRecords[index];
                if (!record.IsVisible)
                {
                    continue;
                }
                if (top == null || CompareRenderOrder(record, top) > 0)
                {
                    top = record;
                }
            }
            return top;
        }

        private void CancelInputMode()
        {
            _inputModeToken.Cancel();
            _inputModeToken = default;
        }

        private static int CompareRenderOrder(ViewRecord left, ViewRecord right)
        {
            var layerComparison = left.SortingOrder.CompareTo(right.SortingOrder);
            return layerComparison != 0 ? layerComparison : left.Token.ViewId.CompareTo(right.Token.ViewId);
        }
    }
}
