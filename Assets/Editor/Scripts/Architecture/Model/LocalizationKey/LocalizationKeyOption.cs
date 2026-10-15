namespace Gameplay.Editor
{
    public readonly struct LocalizationKeyOption
    {
        public readonly string Key;
        public readonly string Text;

        public LocalizationKeyOption(string key, string text)
        {
            Key = key;
            Text = text;
        }

        public string DisplayName => string.IsNullOrEmpty(Text) ? Key : $"{Key} - {Text}";
    }
}
