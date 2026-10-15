using Aoiro;

namespace Gameplay
{
    public readonly struct ProcedurePauseToken : ICancelToken
    {
        public bool IsValid => _system != null && _system.IsPauseTokenActive(_reason, _tokenId);

        private readonly ProcedureSystem _system;
        private readonly ProcedurePauseReason _reason;
        private readonly long _tokenId;

        internal ProcedurePauseToken(ProcedureSystem system, ProcedurePauseReason reason, long tokenId)
        {
            _system = system;
            _reason = reason;
            _tokenId = tokenId;
        }

        public void Cancel()
        {
            _system?.CancelPause(_reason, _tokenId);
        }
    }
}
