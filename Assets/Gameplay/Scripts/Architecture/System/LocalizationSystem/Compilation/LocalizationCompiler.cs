using System;
using System.Collections.Generic;

namespace Gameplay.Localizations
{
    internal sealed class LocalizationCompiler
    {
        private readonly LocalizationResolverRegistry _resolverRegistry;

        internal LocalizationCompiler(LocalizationResolverRegistry resolverRegistry)
        {
            _resolverRegistry = resolverRegistry;
        }

        internal LocalizationToken[] Compile(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('{') < 0)
            {
                return null;
            }

            var tokens = new List<LocalizationToken>();
            var span = text.AsSpan();
            var offset = 0;

            while (offset < span.Length)
            {
                var openRelative = span.Slice(offset).IndexOf('{');
                if (openRelative < 0)
                {
                    AddLiteral(tokens, text, offset, span.Length - offset);
                    break;
                }

                var open = offset + openRelative;
                var closeRelative = span.Slice(open + 1).IndexOf('}');
                if (closeRelative < 0)
                {
                    AddLiteral(tokens, text, offset, span.Length - offset);
                    break;
                }

                var close = open + 1 + closeRelative;
                if (open > offset)
                {
                    AddLiteral(tokens, text, offset, open - offset);
                }

                AddToken(tokens, text, open, close);
                offset = close + 1;
            }

            return tokens.Count == 0 ? null : tokens.ToArray();
        }

        private void AddToken(List<LocalizationToken> tokens, string source, int open, int close)
        {
            var token = source.AsSpan(open + 1, close - open - 1);
            if (token.IsEmpty)
            {
                AddLiteral(tokens, source, open, close - open + 1);
                return;
            }

            if (IsFormatPlaceholder(token))
            {
                AddLiteral(tokens, source, open, close - open + 1, LocalizationTokenKind.FormatPlaceholder);
                return;
            }

            var separator = token.IndexOf(':');
            if (separator > 0)
            {
                var name = token.Slice(0, separator);
                var argument = new LocalizationTextSlice(source, open + 1 + separator + 1, token.Length - separator - 1);
                if (_resolverRegistry.TryGetId(name.ToString(), out var resolverId))
                {
                    var text = new LocalizationTextSlice(source, open, close - open + 1);
                    tokens.Add(new LocalizationToken(LocalizationTokenKind.Function, text, resolverId: resolverId, argument: argument));
                    return;
                }
            }

            tokens.Add(new LocalizationToken(LocalizationTokenKind.TextReference, key: token.ToString()));
        }

        private static void AddLiteral(List<LocalizationToken> tokens, string source, int start, int length, LocalizationTokenKind kind = LocalizationTokenKind.Literal)
        {
            if (length > 0)
            {
                tokens.Add(new LocalizationToken(kind, new LocalizationTextSlice(source, start, length)));
            }
        }

        private static bool IsFormatPlaceholder(ReadOnlySpan<char> token)
        {
            for (var index = 0; index < token.Length; index++)
            {
                if (!char.IsDigit(token[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
