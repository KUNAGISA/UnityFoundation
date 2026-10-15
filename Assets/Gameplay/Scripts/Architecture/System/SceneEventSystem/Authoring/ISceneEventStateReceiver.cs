namespace Gameplay
{
    public interface ISceneEventStateReceiver
    {
        void OnSceneEventStateChanged(bool isTriggerable);
    }
}
