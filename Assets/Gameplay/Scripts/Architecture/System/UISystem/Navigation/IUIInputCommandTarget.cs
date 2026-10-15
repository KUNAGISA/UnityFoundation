namespace Gameplay
{
    public interface IUIInputCommandTarget
    {
        bool TryConsumeUIInputCommand(UIInputCommand command);
    }
}
