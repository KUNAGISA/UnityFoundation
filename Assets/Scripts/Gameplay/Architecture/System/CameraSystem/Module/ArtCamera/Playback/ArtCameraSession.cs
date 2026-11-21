using System.Threading;
using Cysharp.Threading.Tasks;
using Gameplay.Asset;
using UnityEngine;

namespace Gameplay
{
    internal enum ArtCameraSessionState : byte
    {
        Loading,
        Active,
        Holding,
        Deactivating,
    }

    internal sealed class ArtCameraSession
    {
        public long SessionId;
        public ArtCameraLayer Layer;
        public ArtCameraCoveredPolicy CoveredPolicy;
        public ArtCameraCompletionPolicy CompletionPolicy;
        public ArtCameraBinding[] Bindings;
        public ITimeSource ContentTimeSource;
        public int Priority;
        public bool PriorityResolved;
        public float LoadingElapsed;
        public ArtCameraRig Rig;
        public IAssetAsyncOperation<GameObject> AssetOperation;
        public CameraOutputRegistration OutputRegistration;
        public CancellationTokenRegistration LifetimeRegistration;
        public CancellationTokenSource LoadingCancellationSource;
        public UniTaskCompletionSource ReadySource;
        public UniTaskCompletionSource<ArtCameraCompletionResult> CompletionSource;
        public ArtCameraSessionState State;
        public volatile bool CancelRequested;
        public bool CompletionReported;
        public bool CleanedUp;
        public int DeactivationFrame;
        public float DeactivationElapsed;
    }
}
