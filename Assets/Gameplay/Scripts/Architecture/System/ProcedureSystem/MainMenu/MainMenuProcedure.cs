using Cysharp.Threading.Tasks;
using Aoiro;

namespace Gameplay
{
    public class MainMenuProcedure : Procedure
    {
        public override ProcedureType ProcedureType => ProcedureType.MainMenu;
        
        protected override async UniTask OnEnterAsync(IProcedureEnterOptions options)
        {
            var cancellationToken = Context.LifetimeCancellationToken;
            await LoadSceneAsync("StartScene", cancellationToken);
            var mainMenuViewToken = Context.GetSystem<UISystem>().OpenView(new MainMenuView.Data());
            await mainMenuViewToken.WaitForShownAsync(cancellationToken);
        }
        
        protected override async UniTask OnExitAsync()
        {
            await UnloadSceneAsync(Context.LifetimeCancellationToken);
        }
    }
}
