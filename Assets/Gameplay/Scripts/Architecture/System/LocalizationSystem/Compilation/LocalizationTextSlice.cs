using System;
using System.Text;

namespace Gameplay.Localizations
{
    internal readonly struct LocalizationTextSlice
    {
        private readonly string _source;
        private readonly int _start;
        private readonly int _length;

        internal LocalizationTextSlice(string source, int start, int length)
        {
            _source = source;
            _start = start;
            _length = length;
        }

        internal ReadOnlySpan<char> Span => _source == null || _length <= 0 ? ReadOnlySpan<char>.Empty : _source.AsSpan(_start, _length);

        internal void AppendTo(StringBuilder builder)
        {
            if (_source != null && _length > 0)
            {
                builder.Append(_source, _start, _length);
            }
        }

        public override string ToString()
        {
            return _source == null || _length <= 0 ? string.Empty : _source.Substring(_start, _length);
        }
    }
}
