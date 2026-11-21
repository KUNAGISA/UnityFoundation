namespace Gameplay
{
    public enum GameLanguage : byte
    {
        Chinese,
        English,
    }

    public static class GameLanguageCode
    {
        public static string GetCode(this GameLanguage language) => language switch
        {
            GameLanguage.Chinese => "zh",
            GameLanguage.English => "en",
            _ => throw new System.ArgumentOutOfRangeException(nameof(language), language, null),
        };

        public static bool TryParse(string code, out GameLanguage language)
        {
            switch (code)
            {
                case "zh":
                    language = GameLanguage.Chinese;
                    return true;
                case "en":
                    language = GameLanguage.English;
                    return true;
                default:
                    language = default;
                    return false;
            }
        }
    }
}
