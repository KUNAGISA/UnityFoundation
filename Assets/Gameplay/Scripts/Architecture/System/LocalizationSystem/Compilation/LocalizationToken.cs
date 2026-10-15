namespace Gameplay.Localizations
{
    internal enum LocalizationTokenKind
    {
        Literal,
        FormatPlaceholder,
        TextReference,
        Function,
    }

    internal readonly struct LocalizationToken
    {
        internal readonly LocalizationTokenKind Kind;
        internal readonly LocalizationTextSlice Text;
        internal readonly string Key;
        internal readonly int ResolverId;
        internal readonly LocalizationTextSlice Argument;

        internal LocalizationToken(LocalizationTokenKind kind, LocalizationTextSlice text = default, string key = null, int resolverId = 0, LocalizationTextSlice argument = default)
        {
            Kind = kind;
            Text = text;
            Key = key;
            ResolverId = resolverId;
            Argument = argument;
        }
    }
}
