using Aoiro;
using UnityEngine;
using UnityEngine.InputSystem.UI;

namespace Gameplay
{
    public interface IInputService : IService
    {
        PlayerInputChannel Player { get; }
        InputPresentation CurrentPresentation { get; }
        ISignal<UIInputCommand> UICommand { get; }
        ISignal<InputPresentationChanged> InputPresentationChanged { get; }
        bool TryGetPointerPosition(out Vector2 position);
        InputModeToken PushMode(InputModeDefinition mode);
        UIInputModuleToken BindUIInputModule(InputSystemUIInputModule module);
        void Cancel(InputModeToken token);
        void Cancel(UIInputModuleToken token);
    }
}
