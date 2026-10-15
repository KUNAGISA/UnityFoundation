namespace Gameplay.Entities
{
    public interface IAbilityLateTickable
    {
        bool IsEnabled { get; }
        protected internal void LateTick(float deltaTime);
    }
}
