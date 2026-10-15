namespace Gameplay.UI
{
    internal readonly struct UIViewVisibilityChange
    {
        internal UIViewEntry Entry { get; }
        internal bool Visible { get; }

        internal UIViewVisibilityChange(UIViewEntry entry, bool visible)
        {
            Entry = entry;
            Visible = visible;
        }
    }
}
