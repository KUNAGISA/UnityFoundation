namespace Gameplay
{
    public readonly struct LocalizationCatalogChanged
    {
        public GameLanguage Language { get; }
        public TextSourceFlags Sources { get; }

        public LocalizationCatalogChanged(GameLanguage language, TextSourceFlags sources)
        {
            Language = language;
            Sources = sources;
        }
    }
}
