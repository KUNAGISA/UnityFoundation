using System.Collections.Generic;
using Luban;

namespace Gameplay.Localizations
{
    internal sealed class LocalizationCatalog
    {
        internal TextSourceFlags Flags { get; }
        internal string Location { get; private set; } = string.Empty;
        
        private readonly Dictionary<string, LocalizationEntry> _entries = new Dictionary<string, LocalizationEntry>();

        internal LocalizationCatalog(TextSourceFlags flags) => Flags = flags;
        
        internal void Load(string location, ByteBuf buf)
        {
            Location = location;
            _entries.Clear();
            
            var count = buf.ReadSize();
            _entries.EnsureCapacity(count);

            for (var index = 0; index < count; index++)
            {
                var key = buf.ReadString();
                var text = buf.ReadString();
                _entries.Add(key, new LocalizationEntry(text));
            }
        }
        
        internal bool TryGet(string key, out LocalizationEntry entry) => _entries.TryGetValue(key, out entry);

        internal void Clear()
        {
            Location = string.Empty;
            _entries.Clear();
        }
    }
}
