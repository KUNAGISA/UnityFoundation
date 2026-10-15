namespace Gameplay.UI.Editor
{
    internal static class UIBindingIdUtility
    {
        internal static int FromStableKey(string stableKey)
        {
            if (string.IsNullOrEmpty(stableKey))
            {
                return UIBindingProjectSettings.AutoBindingId;
            }

            unchecked
            {
                uint hash = 2166136261;
                for (var index = 0; index < stableKey.Length; index++)
                {
                    hash ^= stableKey[index];
                    hash *= 16777619;
                }

                var id = (int)(hash & int.MaxValue);
                return id == UIBindingProjectSettings.AutoBindingId ? int.MaxValue : id;
            }
        }
    }
}
