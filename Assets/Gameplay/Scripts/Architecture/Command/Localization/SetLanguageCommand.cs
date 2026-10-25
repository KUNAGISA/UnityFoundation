using Aoiro;

namespace Gameplay
{
    public readonly struct SetLanguageCommand : ICommand
    {
        private readonly GameLanguage _language;

        public SetLanguageCommand(GameLanguage language)
        {
            _language = language;
        }

        void ICommand.Execute(ICommandContext context)
        {
            var globalSettingModel = context.GetModel<GlobalSettingModel>();
            if (globalSettingModel.Language == _language)
            {
                return;
            }

            globalSettingModel.SetLanguage(_language);
            context.GetSystem<LocalizationSystem>().Reload();
        }
    }
}
