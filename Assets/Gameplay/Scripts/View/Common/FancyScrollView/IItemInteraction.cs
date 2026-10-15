namespace Gameplay
{
    public interface IItemInteraction
    {
        bool TrySelectItem(int index, bool scrollIntoView);
        bool TrySubmitItem(int index);
    }
}
