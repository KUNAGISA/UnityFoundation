using Aoiro;
using UnityEngine.InputSystem.UI;

namespace Gameplay
{
    public readonly struct UIInputModuleToken : ICancelToken
    {
        internal IInputService Service { get; }
        internal InputSystemUIInputModule Module { get; }

        public bool IsValid => Service != null && Module != null;

        internal UIInputModuleToken(IInputService service, InputSystemUIInputModule module)
        {
            Service = service;
            Module = module;
        }

        public void Cancel()
        {
            Service?.Cancel(this);
        }
    }
}
