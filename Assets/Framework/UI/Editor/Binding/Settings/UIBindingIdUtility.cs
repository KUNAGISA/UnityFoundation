namespace Gameplay.UI.Editor
{
    internal static class UIBindingIdUtility
    {
        /// <summary>以固定散列规则生成绑定 ID，避免进程变化影响结果；修改稳定键会使已有绑定引用失效，空键表示自动绑定。</summary>
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
