namespace Gameplay
{
    public interface IInteractionSource
    {
        string DisplayText { get; }
        int Priority { get; }
        bool CanInteract();
        InteractionResult Interact();
    }
}
