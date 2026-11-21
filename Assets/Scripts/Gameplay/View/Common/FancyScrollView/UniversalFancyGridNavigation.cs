using FancyScrollView.Universal;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay
{
    [RequireComponent(typeof(UniversalFancyGridView))]
    public sealed class UniversalFancyGridNavigation : UniversalFancyNavigation
    {
        [SerializeField]
        private UniversalFancyGridView _gridView = null;

        [SerializeField]
        private UINavigationAxis _scrollDirection = UINavigationAxis.Vertical;

        protected override UniversalScrollContext SharedContext => _gridView.SharedContext;
        protected override int ItemCount => _gridView.Count;

        protected override void Awake()
        {
            base.Awake();
            if (_gridView == null)
            {
                _gridView = GetComponent<UniversalFancyGridView>();
            }
        }

        protected override bool TryMove(MoveDirection direction)
        {
            if (!SharedContext.TryGet<ISelection>(out var selection) || selection.SelectedIndex < 0)
            {
                RefreshSelection();
                return true;
            }

            var current = selection.SelectedIndex;
            var count = Mathf.Max(1, _gridView.StartAxisCellCount);
            if (_scrollDirection == UINavigationAxis.Vertical)
            {
                return direction switch
                {
                    MoveDirection.Left => TryMoveByStep(-1, index => index / count == current / count),
                    MoveDirection.Right => TryMoveByStep(1, index => index / count == current / count),
                    MoveDirection.Up => TryMoveByStep(-count),
                    MoveDirection.Down => TryMoveByStep(count),
                    _ => false
                };
            }
            return direction switch
            {
                MoveDirection.Up => TryMoveByStep(-1, index => index / count == current / count),
                MoveDirection.Down => TryMoveByStep(1, index => index / count == current / count),
                MoveDirection.Left => TryMoveByStep(-count),
                MoveDirection.Right => TryMoveByStep(count),
                _ => false
            };
        }

        protected override bool HandlesDirection(MoveDirection direction) => direction != MoveDirection.None;

        protected override void RefreshItems() => _gridView.RefreshItems();

        protected override void ScrollToItem(int index, float duration, float alignment) => _gridView.ScrollToItem(index, duration, alignment);
    }
}
