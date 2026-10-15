using Aoiro;
using Gameplay.Tables;

namespace Gameplay.Localizations
{
    internal sealed class ItemNameLocalizationResolver : ILocalizationTokenResolver
    {
        public bool TryResolve(LocalizationTextSlice argument, LocalizationResolveContext context, out string result)
        {
            result = null;

            if (!int.TryParse(argument.Span, out var itemId))
            {
                return false;
            }
            
            if (!context.GetModel<TablesModel>().ItemTable.TryGet(itemId, out var item))
            {
                return false;
            }

            result = item.Name;
            return true;
        }
    }
}
