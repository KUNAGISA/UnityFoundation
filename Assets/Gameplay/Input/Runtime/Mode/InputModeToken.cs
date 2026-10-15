using Aoiro;

namespace Gameplay
{
    public readonly struct InputModeToken : ICancelToken
    {
        internal IInputService Service { get; }
        internal long EntryId { get; }

        public bool IsValid => Service != null && EntryId != 0;

        internal InputModeToken(IInputService service, long entryId)
        {
            Service = service;
            EntryId = entryId;
        }

        public void Cancel()
        {
            Service?.Cancel(this);
        }
    }
}
