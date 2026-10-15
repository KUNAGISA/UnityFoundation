namespace Gameplay
{
    public enum ArtCameraCompletionReason : byte
    {
        Completed,
        Cancelled,
        CoveredAndCancelled,
        TargetLost,
    }
    
    public readonly struct ArtCameraCompletionResult
    {
        public readonly long SessionId;
        public readonly ArtCameraCompletionReason Reason;
        public readonly float FinalNormalizedTime;

        public ArtCameraCompletionResult(long sessionId, ArtCameraCompletionReason reason, float finalNormalizedTime)
        {
            SessionId = sessionId;
            Reason = reason;
            FinalNormalizedTime = finalNormalizedTime;
        }
    }
}