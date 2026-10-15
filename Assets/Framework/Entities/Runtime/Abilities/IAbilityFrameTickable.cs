namespace Gameplay.Entities
{
    public interface IAbilityFrameTickable
    {
        bool IsEnabled { get; }
        protected internal void FrameTick(float deltaTime);
    }
}
