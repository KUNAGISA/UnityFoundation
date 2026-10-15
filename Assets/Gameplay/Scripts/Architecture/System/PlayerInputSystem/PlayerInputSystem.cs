using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Scheduler;

namespace Gameplay
{
    public class PlayerInputSystem : ProcedureCapacitySystem, ISceneBeforeLeave
    {
        public bool IsActive { get; private set; } = false;

        private InputModeToken _inputModeToken;
        private FrameTickToken _earlyTickToken = default;
        private FrameTickToken _lateTickToken = default;
        private PlayerInputChannel _playerInputChannel = null;

        private PlayerEntity _target = null;
        private PlayerInputState _targetInput = null;

        protected override void OnInitialize()
        {
            _playerInputChannel = this.GetService<IInputService>().Player;
        }

        protected override void OnShutdown()
        {
            _inputModeToken.Cancel();
            _inputModeToken = default;
            _earlyTickToken.Cancel();
            _lateTickToken.Cancel();
            _earlyTickToken = default;
            _lateTickToken = default;
            _playerInputChannel = null;
        }

        public void Bind(PlayerEntity target)
        {
            Unbind();
            _target = target ?? throw new System.ArgumentNullException(nameof(target));
            _targetInput = target.InputState;
            Recompute();
        }

        public void Unbind()
        {
            _targetInput?.Reset();
            _target = null;
            _targetInput = null;
        }

        UniTask ISceneBeforeLeave.BeforeLeaveSceneAsync(CancellationToken cancellationToken)
        {
            Unbind();
            return UniTask.CompletedTask;
        }

        protected override void OnProcedureAttach(IProcedureContext context)
        {
            IsActive = true;
            _earlyTickToken = context.AddFrameTickTask(OnProcedureEarlyTick, ProcedureTiming.EarlyTick);
            _lateTickToken = context.AddFrameTickTask(OnProcedureLateTick, ProcedureTiming.LateTick);
            Recompute();
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            IsActive = false;
            _earlyTickToken.Cancel();
            _lateTickToken.Cancel();
            _earlyTickToken = default;
            _lateTickToken = default;
            Unbind();
            Recompute();
        }

        private void Recompute()
        {
            var enable = IsActive && _target != null;
            if (enable)
            {
                if (!_inputModeToken.IsValid)
                {
                    _inputModeToken = this.GetService<IInputService>().PushMode(InputModeCatalog.PlayerControl);
                }
            }
            else
            {
                _inputModeToken.Cancel();
                _inputModeToken = default;
            }
        }

        private void OnProcedureEarlyTick(in FrameTime time)
        {
            if (_targetInput != null)
            {
                _targetInput.InputDir = _playerInputChannel.Move;
            }
        }

        private void OnProcedureLateTick(in FrameTime time)
        {
            _playerInputChannel?.ResetTransient();
        }
    }
}
