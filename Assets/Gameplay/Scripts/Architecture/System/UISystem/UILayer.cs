using Gameplay.UI;

namespace Gameplay
{
    public enum UILayer
    {
        HUD = 0,
        View = 1,
        Adv = 2,
        Popup = 3,
        Loading = 4,
        Modal = 5,
        Overlay = 6
    }

    public static class UILayerExtensions
    {
        public static UILayerId ToLayerId(this UILayer layer) => new UILayerId((int)layer);
    }
}
