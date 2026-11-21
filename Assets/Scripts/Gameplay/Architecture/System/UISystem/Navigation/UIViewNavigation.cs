using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIFocusScope))]
    public sealed class UIViewNavigation : MonoBehaviour
    {
        private sealed class FocusScopeEntry
        {
            public int Id;
            public UIFocusScope Scope;
            public IUIInputCommandTarget CommandTarget;
            public Selectable ReturnFocus;
        }

        private readonly List<FocusScopeEntry> _stack = new();
        private readonly UIPointerNavigationResolver _pointerResolver = new();
        private int _scopeId;
        private bool _navigationInputActive = true;

        public bool IsActive => enabled;
        private UIFocusScope CurrentFocusScope => _stack.Count > 0 ? _stack[^1].Scope : null;

        private void Awake()
        {
            var scope = GetComponent<UIFocusScope>();
            _stack.Add(new FocusScopeEntry
            {
                Scope = scope,
                CommandTarget = scope.GetComponent<IUIInputCommandTarget>()
            });
            enabled = false;
        }

        private void LateUpdate()
        {
            var current = CurrentFocusScope;
            var selected = GetCurrentSelectable();
            if (!_navigationInputActive)
            {
                RememberAndClearFocus();
                return;
            }
            if (current.Contains(selected) && UIFocusScope.IsSelectable(selected))
            {
                current.Remember(selected);
                return;
            }

            SetFocus(current.ResolveFocus());
        }

        public UIFocusScopeToken PushFocusScope(GameObject scopeRoot)
        {
            return PushResolvedFocusScope(scopeRoot != null ? scopeRoot.GetComponent<UIFocusScope>() : null);
        }

        public UIFocusScopeToken PushFocusScope(Component scopeRoot)
        {
            return PushResolvedFocusScope(scopeRoot != null ? scopeRoot.GetComponent<UIFocusScope>() : null);
        }

        public bool IsCurrentFocusScope(GameObject scopeRoot)
        {
            return scopeRoot != null && ReferenceEquals(CurrentFocusScope, scopeRoot.GetComponent<UIFocusScope>());
        }

        public bool IsCurrentFocusScope(Component scopeRoot)
        {
            return scopeRoot != null && ReferenceEquals(CurrentFocusScope, scopeRoot.GetComponent<UIFocusScope>());
        }

        private UIFocusScopeToken PushResolvedFocusScope(UIFocusScope scope)
        {
            if (scope == null || !scope.transform.IsChildOf(transform) || ReferenceEquals(scope, CurrentFocusScope))
            {
                return default;
            }

            var current = CurrentFocusScope;
            var selected = GetCurrentSelectable();
            current.Remember(selected);

            var entry = new FocusScopeEntry
            {
                Id = ++_scopeId,
                Scope = scope,
                CommandTarget = scope.GetComponent<IUIInputCommandTarget>(),
                ReturnFocus = current.Contains(selected) ? selected : current.ResolveFocus()
            };
            _stack.Add(entry);
            if (enabled)
            {
                SetFocus(scope.ResolveFocus());
            }
            return new UIFocusScopeToken(this, entry.Id);
        }

        public void Focus(Selectable selectable)
        {
            var current = CurrentFocusScope;
            if (current == null || !current.Contains(selectable))
            {
                return;
            }
            var resolved = current.ResolveFocus(selectable);
            current.Remember(resolved);
            if (enabled)
            {
                SetFocus(resolved);
            }
        }

        public void RefreshFocus(Selectable preferred = null)
        {
            var current = CurrentFocusScope;
            var resolved = current.ResolveFocus(preferred);
            current.Remember(resolved);
            if (enabled)
            {
                SetFocus(resolved);
            }
        }

        internal void TakeNavigationControl(Vector2 pointerPosition, bool suppressPointerHover)
        {
            _navigationInputActive = true;
            _pointerResolver.ResolveTargets(transform, CurrentFocusScope, pointerPosition, out var hovered, out var navigable);
            if (navigable != null)
            {
                Focus(navigable);
            }
            else
            {
                RefreshFocus();
            }

            if (suppressPointerHover && hovered != null)
            {
                _pointerResolver.NotifyPointerExit(hovered, pointerPosition);
            }
        }

        internal void TakePointerControl(Vector2 pointerPosition, bool supportsHover)
        {
            _navigationInputActive = false;
            RememberAndClearFocus();
            if (!supportsHover)
            {
                return;
            }

            _pointerResolver.ResolveTargets(transform, CurrentFocusScope, pointerPosition, out var hovered, out _);
            if (hovered != null)
            {
                _pointerResolver.NotifyPointerEnter(hovered, pointerPosition);
            }
        }

        internal void SetInteractionSource(UIInteractionSource source)
        {
            _navigationInputActive = source == UIInteractionSource.Navigation;
            if (!enabled)
            {
                return;
            }
            if (_navigationInputActive)
            {
                RefreshFocus(GetCurrentSelectable());
                return;
            }

            RememberAndClearFocus();
        }

        internal void SetRouteActive(bool active)
        {
            if (enabled == active)
            {
                if (active)
                {
                    RefreshFocus(GetCurrentSelectable());
                }
                return;
            }

            var current = CurrentFocusScope;
            if (!active)
            {
                RememberAndClearFocus();
            }
            enabled = active;
            if (active)
            {
                SetFocus(current.ResolveFocus());
            }
        }

        internal void RouteCommand(UIInputCommand command)
        {
            if (!enabled)
            {
                return;
            }

            for (var index = _stack.Count - 1; index >= 0; index--)
            {
                var commandTarget = _stack[index].CommandTarget;
                if (commandTarget != null && commandTarget.TryConsumeUIInputCommand(command))
                {
                    return;
                }
            }
            if (command == UIInputCommand.Cancel && _stack.Count > 1)
            {
                Cancel(new UIFocusScopeToken(this, _stack[^1].Id));
            }
        }

        internal void Cancel(UIFocusScopeToken token)
        {
            if (!ReferenceEquals(token.Navigation, this) || token.ScopeId <= 0)
            {
                return;
            }

            var removeIndex = -1;
            for (var index = 1; index < _stack.Count; index++)
            {
                if (_stack[index].Id == token.ScopeId)
                {
                    removeIndex = index;
                    break;
                }
            }
            if (removeIndex < 0)
            {
                return;
            }

            var returnFocus = _stack[removeIndex].ReturnFocus;
            for (var index = _stack.Count - 1; index >= removeIndex; index--)
            {
                _stack.RemoveAt(index);
            }

            var parent = CurrentFocusScope;
            if (enabled)
            {
                SetFocus(parent.ResolveFocus(returnFocus));
            }
        }

        internal void Release()
        {
            if (_stack.Count == 0)
            {
                return;
            }
            SetRouteActive(false);
            _stack.Clear();
        }

        private static Selectable GetCurrentSelectable()
        {
            var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
            return selected != null ? selected.GetComponent<Selectable>() : null;
        }

        private static void SetFocus(Selectable selectable)
        {
            EventSystem.current?.SetSelectedGameObject(selectable != null ? selectable.gameObject : null);
        }

        private void ClearViewSelection(Selectable selected)
        {
            if (selected != null && selected.transform.IsChildOf(transform))
            {
                EventSystem.current?.SetSelectedGameObject(null);
            }
        }

        private void RememberAndClearFocus()
        {
            var current = CurrentFocusScope;
            var selected = GetCurrentSelectable();
            current.Remember(selected);
            ClearViewSelection(selected);
        }
    }
}
