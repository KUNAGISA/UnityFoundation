namespace Gameplay.Localizations
{
    internal interface ILocalizationTokenResolver
    {
        bool TryResolve(LocalizationTextSlice argument, LocalizationResolveContext context, out string result);
    }
}
