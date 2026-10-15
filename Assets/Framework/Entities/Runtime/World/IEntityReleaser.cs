namespace Gameplay.Entities
{
    public interface IEntityReleaser
    {
        void Release(in EntityReleaseContext context);
    }
}
