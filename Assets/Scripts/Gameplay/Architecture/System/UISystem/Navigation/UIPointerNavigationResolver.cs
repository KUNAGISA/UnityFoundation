using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay
{
    internal sealed class UIPointerNavigationResolver
    {
        private readonly List<RaycastResult> _raycastResults = new();

        internal void ResolveTargets(Transform viewRoot, UIFocusScope focusScope, Vector2 pointerPosition, out Selectable hovered, out Selectable navigable)
        {
            hovered = null;
            navigable = null;
            var eventSystem = EventSystem.current;
            if (eventSystem == null || viewRoot == null || focusScope == null)
            {
                return;
            }

            _raycastResults.Clear();
            eventSystem.RaycastAll(CreatePointerEventData(eventSystem, pointerPosition), _raycastResults);
            for (var resultIndex = 0; resultIndex < _raycastResults.Count; resultIndex++)
            {
                var target = _raycastResults[resultIndex].gameObject != null ? _raycastResults[resultIndex].gameObject.transform : null;
                if (target == null || !target.IsChildOf(viewRoot))
                {
                    continue;
                }

                for (var node = target; node != null && node.IsChildOf(viewRoot); node = node.parent)
                {
                    var selectable = node.GetComponent<Selectable>();
                    if (selectable == null)
                    {
                        continue;
                    }
                    hovered ??= selectable;
                    if (focusScope.Contains(selectable) && UIFocusScope.IsSelectable(selectable))
                    {
                        navigable = selectable;
                        _raycastResults.Clear();
                        return;
                    }
                }

                if (hovered != null)
                {
                    break;
                }
            }
            _raycastResults.Clear();
        }

        internal void NotifyPointerEnter(Selectable selectable, Vector2 pointerPosition)
        {
            selectable.OnPointerEnter(CreatePointerEventData(EventSystem.current, pointerPosition));
        }

        internal void NotifyPointerExit(Selectable selectable, Vector2 pointerPosition)
        {
            selectable.OnPointerExit(CreatePointerEventData(EventSystem.current, pointerPosition));
        }

        private static PointerEventData CreatePointerEventData(EventSystem eventSystem, Vector2 pointerPosition)
        {
            return new PointerEventData(eventSystem)
            {
                position = pointerPosition
            };
        }
    }
}
