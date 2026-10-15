using System.Collections.Generic;

namespace Gameplay.Localizations
{
    internal sealed class LocalizationResolverRegistry
    {
        private readonly List<ILocalizationTokenResolver> _resolvers = new List<ILocalizationTokenResolver>();
        private readonly Dictionary<string, int> _nameToId = new Dictionary<string, int>();

        internal int Register(string name, ILocalizationTokenResolver resolver)
        {
            if (_nameToId.TryGetValue(name, out var id))
            {
                _resolvers[id - 1] = resolver;
                return id;
            }

            id = _resolvers.Count + 1;
            _nameToId.Add(name, id);
            _resolvers.Add(resolver);
            return id;
        }

        internal bool TryGetId(string name, out int id) => _nameToId.TryGetValue(name, out id);

        internal bool TryResolve(int id, LocalizationTextSlice argument, LocalizationResolveContext context, out string result)
        {
            if (id <= 0 || id > _resolvers.Count)
            {
                result = null;
                return false;
            }

            return _resolvers[id - 1].TryResolve(argument, context, out result);
        }
    }
}
