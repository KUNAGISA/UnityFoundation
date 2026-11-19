namespace Gameplay
{
    public readonly struct PlayerCameraActivateOptions
    {
        public readonly PlayerActor Player;

        public PlayerCameraActivateOptions(PlayerActor player)
        {
            Player = player;
        }
    }
}
