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
        private const string PreviewSceneIdKey = "Gameplay.Editor.StartupSceneToolbar.PreviewSceneId";

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
                Options = new LaunchProcedureOptions(this, ConsumePreviewSceneId()),
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

        private static int? ConsumePreviewSceneId()
        {
#if UNITY_EDITOR
            var sceneId = SessionState.GetInt(PreviewSceneIdKey, 0);
            SessionState.EraseInt(PreviewSceneIdKey);
            return sceneId > 0 ? sceneId : null;
#else
            return null;
#endif
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
