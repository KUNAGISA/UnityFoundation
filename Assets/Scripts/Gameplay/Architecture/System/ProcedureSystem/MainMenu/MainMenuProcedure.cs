using System;
using Gameplay.UI;
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
            if (!await mainMenuViewToken.WaitForShownAsync(cancellationToken))
            {
                throw new InvalidOperationException("Main menu view did not finish showing.");
            }
        }
        
        protected override async UniTask OnExitAsync()
        {
            await UnloadSceneAsync(Context.LifetimeCancellationToken);
        }
    }
}
