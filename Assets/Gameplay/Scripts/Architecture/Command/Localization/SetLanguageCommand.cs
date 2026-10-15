using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;

namespace Gameplay
{
    public readonly struct SetLanguageCommand : ICommand<UniTask>
    {
        private readonly GameLanguage _language;
        private readonly CancellationToken _cancellationToken;

        public SetLanguageCommand(GameLanguage language, CancellationToken cancellationToken = default)
        {
            _language = language;
            _cancellationToken = cancellationToken;
        }

        UniTask ICommand<UniTask>.Execute(ICommandContext context) => ExecuteAsync(context);

        private async UniTask ExecuteAsync(ICommandContext context)
        {
            var globalSettingModel = context.GetModel<GlobalSettingModel>();
            if (globalSettingModel.Language == _language)
            {
                return;
            }

            var procedureSystem = context.GetSystem<ProcedureSystem>();
            var localizationSystem = context.GetSystem<LocalizationSystem>();
            var inputModeToken = context.GetService<IInputService>().PushMode(InputModeCatalog.LanguageSwitch);
            var pauseToken = default(ProcedurePauseToken);

            try
            {
                pauseToken = procedureSystem.Pause(ProcedurePauseReason.LanguageSwitch);
                await globalSettingModel.SetLanguageAsync(_language, CancellationToken.None);
                await localizationSystem.ReloadAsync(_cancellationToken);
            }
            finally
            {
                pauseToken.Cancel();
                inputModeToken.Cancel();
            }
        }
    }
}
