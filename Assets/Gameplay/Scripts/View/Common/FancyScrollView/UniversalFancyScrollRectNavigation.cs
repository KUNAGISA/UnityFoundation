using FancyScrollView.Universal;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Gameplay
{
    [RequireComponent(typeof(UniversalFancyScrollRect))]
    public sealed class UniversalFancyScrollRectNavigation : UniversalFancyNavigation
    {
        [SerializeField]
        private UniversalFancyScrollRect _scrollRect = null;

        [SerializeField]
        private UINavigationAxis _direction = UINavigationAxis.Vertical;

        protected override UniversalScrollContext SharedContext => _scrollRect.SharedContext;
        protected override int ItemCount => _scrollRect.Count;

        protected override void Awake()
        {
            base.Awake();
            if (_scrollRect == null)
            {
                _scrollRect = GetComponent<UniversalFancyScrollRect>();
            }
        }

        protected override bool TryMove(MoveDirection direction)
        {
            if (_direction == UINavigationAxis.Vertical)
            {
                return direction switch
                {
                    MoveDirection.Up => TryMoveByStep(-1),
                    MoveDirection.Down => TryMoveByStep(1),
                    _ => false
                };
            }
            return direction switch
            {
                MoveDirection.Left => TryMoveByStep(-1),
                MoveDirection.Right => TryMoveByStep(1),
                _ => false
            };
        }

        protected override bool HandlesDirection(MoveDirection direction)
        {
            return _direction == UINavigationAxis.Vertical ? direction is MoveDirection.Up or MoveDirection.Down : direction is MoveDirection.Left or MoveDirection.Right;
        }

        protected override void RefreshItems() => _scrollRect.RefreshItems();

        protected override void ScrollToItem(int index, float duration, float alignment) => _scrollRect.ScrollToItem(index, duration, alignment);
    }
}
