namespace Gameplay
{
    public readonly struct PlayerCameraActivateOptions
    {
        public readonly PlayerEntity Player;

        public PlayerCameraActivateOptions(PlayerEntity player)
        {
            Player = player;
        }
    }
}
