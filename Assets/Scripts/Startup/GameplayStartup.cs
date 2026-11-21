using System;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Gameplay;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Startup
{
    public sealed class GameplayStartup : MonoBehaviour, IGameplayController, IStartupSplash
    {
#if UNITY_EDITOR
        private const string PreviewSceneIdKey = "Gameplay.Editor.StartupSceneToolbar.PreviewSceneId";
        private const string PreviewPositionKey = "Gameplay.Editor.StartupSceneToolbar.PreviewPosition";
#endif

        [SerializeField]
        private GameObject _splash;
        [SerializeField]
        private Image _background;

        private SubscriptionToken _successSubscription;

        private void Awake()
        {
            if (_splash == null || _background == null)
            {
                throw new InvalidOperationException("GameplayStartup requires serialized splash and background references.");
            }
        }

        private void Start()
        {
            _successSubscription = this.SubscribeEvent<EnterProcedureSuccessEvent>(OnProcedureEntered);
            this.GetSystem<ProcedureSystem>().Transition(new TransitionProcedureRequest(ProcedureType.Launch)
            {
                Reason = TransitionProcedureReason.Startup,
                Options = CreateLaunchOptions(),
            });
        }

        private void OnDestroy()
        {
            CancelSubscriptions();
        }

        UniTask IStartupSplash.WaitForSplashAsync(CancellationToken cancellationToken)
        {
            return UniTask.CompletedTask;
        }

        void IStartupSplash.HideSplash()
        {
            _background.DOFade(0f, 0.5f);
        }

        private void CancelSubscriptions()
        {
            _successSubscription.Cancel();
            _successSubscription = default;
        }

        private LaunchProcedureOptions CreateLaunchOptions()
        {
#if UNITY_EDITOR
            var sceneId = SessionState.GetInt(PreviewSceneIdKey, 0);
            var positionJson = SessionState.GetString(PreviewPositionKey, string.Empty);
            SessionState.EraseInt(PreviewSceneIdKey);
            SessionState.EraseString(PreviewPositionKey);
            if (sceneId > 0)
            {
                Vector2? position = string.IsNullOrEmpty(positionJson) ? null : JsonUtility.FromJson<Vector2>(positionJson);
                return new LaunchProcedureOptions(this, sceneId, position);
            }
#endif
            return new LaunchProcedureOptions(this);
        }

        private void OnProcedureEntered(EnterProcedureSuccessEvent evt)
        {
            if (evt.ProcedureType != ProcedureType.MainMenu && evt.ProcedureType != ProcedureType.Game)
            {
                return;
            }

            CancelSubscriptions();
            SceneManager.UnloadSceneAsync(gameObject.scene);
        }
    }
}
