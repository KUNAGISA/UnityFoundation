namespace Gameplay
{
    public static class InputModeCatalog
    {
        public static readonly InputModeDefinition Game = new(0, InputChannelMask.HotKey | InputChannelMask.UIGlobal, InputChannelMask.UIModal);
        public static readonly InputModeDefinition PlayerControl = new(0, InputChannelMask.Player, InputChannelMask.None);
        public static readonly InputModeDefinition Sequence = new(50, InputChannelMask.None, InputChannelMask.Player | InputChannelMask.HotKey);
        public static readonly InputModeDefinition View = new(100, InputChannelMask.UIModal, InputChannelMask.Player | InputChannelMask.HotKey);
        public static readonly InputModeDefinition UINavigation = new(150, InputChannelMask.UIModal, InputChannelMask.None);
        public static readonly InputModeDefinition Pause = new(200, InputChannelMask.None, InputChannelMask.Player | InputChannelMask.HotKey);
        public static readonly InputModeDefinition Console = new(300, InputChannelMask.UIGlobal, InputChannelMask.Player | InputChannelMask.HotKey | InputChannelMask.UIModal);
        public static readonly InputModeDefinition LanguageSwitch = new(400, InputChannelMask.None, InputChannelMask.All);
    }
}
