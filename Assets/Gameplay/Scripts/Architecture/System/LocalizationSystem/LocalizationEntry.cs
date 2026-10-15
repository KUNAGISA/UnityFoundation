namespace Gameplay.Localizations
{
    internal sealed class LocalizationEntry
    {
        internal string Text { get; }

        private LocalizationToken[] _tokens;
        private bool _isCompiled;

        internal LocalizationEntry(string text)
        {
            Text = text;
        }

        internal LocalizationToken[] GetTokens(LocalizationCompiler compiler)
        {
            if (!_isCompiled)
            {
                _tokens = compiler.Compile(Text);
                _isCompiled = true;
            }

            return _tokens;
        }
    }
}
