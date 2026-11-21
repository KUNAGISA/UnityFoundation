using Cysharp.Threading.Tasks;

namespace Gameplay
{
    /// <summary>
    /// ArtCamera 播放会话。ReadyTask 在 Rig 准备完成或会话在准备前正常结束时完成，CompletionTask 返回最终播放结果。
    /// </summary>
    public readonly struct ArtCameraPlayback
    {
        public readonly long SessionId;
        public readonly CameraSessionToken Token;
        public readonly UniTask ReadyTask;
        public readonly UniTask<ArtCameraCompletionResult> CompletionTask;

        internal ArtCameraPlayback(long sessionId, CameraSessionToken token, UniTask readyTask, UniTask<ArtCameraCompletionResult> completionTask)
        {
            SessionId = sessionId;
            Token = token;
            ReadyTask = readyTask;
            CompletionTask = completionTask;
        }
    }
}
