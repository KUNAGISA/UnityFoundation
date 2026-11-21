using System;
using FancyScrollView.Universal;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Gameplay
{
    public abstract class UniversalFancyNavigation : Selectable, ISubmitHandler, IItemInteraction
    {
        private const float ScrollDuration = 0.15f;
        private const float ScrollAlignment = 0.5f;

        protected abstract UniversalScrollContext SharedContext { get; }
        protected abstract int ItemCount { get; }

        protected override void OnEnable()
        {
            base.OnEnable();
            SharedContext.Set<IItemInteraction>(this);
        }

        public override void OnSelect(BaseEventData eventData)
        {
            base.OnSelect(eventData);
            EnsureSelection();
        }

        public override void OnMove(AxisEventData eventData)
        {
            if (!IsActive() || !IsInteractable())
            {
                return;
            }
            if (TryMove(eventData.moveDir))
            {
                eventData.Use();
                return;
            }
            if (HandlesDirection(eventData.moveDir))
            {
                eventData.Use();
            }
            else
            {
                base.OnMove(eventData);
            }
        }

        public void OnSubmit(BaseEventData eventData)
        {
            if (!TryGetSelection(out var selection) || !TrySubmitItem(selection.SelectedIndex))
            {
                return;
            }

            eventData.Use();
        }

        public bool TrySelectItem(int index, bool scrollIntoView)
        {
            if (!IsActive() || !IsInteractable())
            {
                return false;
            }
            if (!TryGetSelection(out var selection))
            {
                selection = InstallSelection();
            }
            if (!selection.CanSelect(SharedContext, index))
            {
                return false;
            }

            if (selection.SelectedIndex != index)
            {
                selection.TrySelect(SharedContext, index);
                RefreshItems();
            }
            if (scrollIntoView)
            {
                ScrollToItem(index, ScrollDuration, ScrollAlignment);
            }
            return true;
        }

        public bool TrySubmitItem(int index)
        {
            if (!TrySelectItem(index, true))
            {
                return false;
            }

            Select();
            if (SharedContext.TryGet<ItemActivation>(out var activation))
            {
                activation.Activate(index);
                if (this != null && isActiveAndEnabled)
                {
                    RefreshItems();
                }
            }
            return true;
        }

        public void RefreshSelection()
        {
            if (TryGetSelection(out var selection))
            {
                selection.Validate(SharedContext);
            }
            if (SharedContext.TryGet<ItemActivation>(out var activation))
            {
                activation.Validate(ItemCount);
            }
            EnsureSelection(true);
        }

        protected abstract bool TryMove(MoveDirection direction);
        protected abstract bool HandlesDirection(MoveDirection direction);
        protected abstract void RefreshItems();
        protected abstract void ScrollToItem(int index, float duration, float alignment);

        protected bool TryMoveByStep(int step, Func<int, bool> withinBoundary = null)
        {
            if (!TryGetSelection(out var selection))
            {
                selection = InstallSelection();
            }
            var current = selection.SelectedIndex;
            if (current < 0)
            {
                return EnsureSelection();
            }

            for (var index = current + step; index >= 0 && index < ItemCount; index += step)
            {
                if (withinBoundary != null && !withinBoundary(index))
                {
                    return false;
                }
                if (selection.CanSelect(SharedContext, index))
                {
                    return SelectItem(index, selection);
                }
            }
            return false;
        }

        private bool EnsureSelection(bool refreshItems = false)
        {
            if (!TryGetSelection(out var selection))
            {
                if (ItemCount <= 0)
                {
                    if (refreshItems)
                    {
                        RefreshItems();
                    }
                    return false;
                }
                selection = InstallSelection();
            }
            if (ItemCount > 0 && selection.CanSelect(SharedContext, selection.SelectedIndex))
            {
                if (refreshItems)
                {
                    RefreshItems();
                }
                return true;
            }
            for (var index = 0; index < ItemCount; index++)
            {
                if (selection.CanSelect(SharedContext, index))
                {
                    return SelectItem(index, selection);
                }
            }

            var selectionChanged = selection.ClearSelection();
            if (selectionChanged || refreshItems)
            {
                RefreshItems();
            }
            return false;
        }

        private bool SelectItem(int index, ISelection selection)
        {
            if (!selection.TrySelect(SharedContext, index))
            {
                return false;
            }
            RefreshItems();
            ScrollToItem(index, ScrollDuration, ScrollAlignment);
            return true;
        }

        private bool TryGetSelection(out ISelection selection)
        {
            selection = null;
            return SharedContext != null && SharedContext.TryGet(out selection);
        }

        private ISelection InstallSelection()
        {
            var selection = new SingleSelection();
            SharedContext.Set<ISelection>(selection);
            return selection;
        }
    }
}
