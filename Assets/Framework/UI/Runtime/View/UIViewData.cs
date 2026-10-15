namespace Gameplay.UI
{
    public abstract class UIViewData
    {
        public abstract string Location { get; }
        public virtual UILayerId LayerId => UILayerId.Unspecified;
        public virtual UIViewOptionMask Options => default;
        public virtual bool AllowMultiple => false;
        public virtual bool HideViewsBelow => false;
    }
}
