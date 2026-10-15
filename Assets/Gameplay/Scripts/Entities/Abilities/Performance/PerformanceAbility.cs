using System;
using Aoiro;
using Gameplay.Entities;
using Gameplay.Tags;
using Gameplay.Tables;
using UnityEngine;

namespace Gameplay
{
    public sealed class PerformanceAbility : Ability, IAbilityFrameTickable
    {
        public PerformanceStatus CurrentStatus => _status;

        private readonly GameplayTagContainer _gameplayTags;
        private readonly SpatialState _spatial;
        private PerformanceContext _performanceContext;
        private IPerformanceNode _runningNode;
        private GameplayTagToken _blockMovementToken;
        private PerformanceStatus _status;
        private int _executionId;

        public PerformanceAbility(GameplayTagContainer gameplayTags, SpatialState spatial)
        {
            _gameplayTags = gameplayTags ?? throw new ArgumentNullException(nameof(gameplayTags));
            _spatial = spatial ?? throw new ArgumentNullException(nameof(spatial));
        }

        protected override void OnInitialize()
        {
            _performanceContext = new PerformanceContext(Owner, Context, _spatial);
        }

        protected override void OnDisabled()
        {
            CancelCurrent();
        }

        protected override void OnDestroy()
        {
            CancelCurrent();
            _performanceContext = default;
        }

        public PerformanceToken Start(IPerformanceNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            if (!IsEnabled)
            {
                Debug.LogWarning($"[Performance] Entity [{Owner.EntityId}] performance ability is disabled.");
                return PerformanceToken.None;
            }

            if (_status == PerformanceStatus.Running)
            {
                Debug.LogWarning($"[Performance] Entity [{Owner.EntityId}] already has a running performance node.");
                return PerformanceToken.None;
            }

            GenerateExecutionId();
            var token = new PerformanceToken(this, _executionId);
            var startingExecutionId = _executionId;
            _status = PerformanceStatus.Running;
            _runningNode = node;
            try
            {
                _gameplayTags.AddTag(GameplayTags.Control.Block.Movement.Tag, out _blockMovementToken);
                if (_executionId != startingExecutionId || _status != PerformanceStatus.Running)
                {
                    return token;
                }
                node.Enter(_performanceContext);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (_executionId == startingExecutionId && _status == PerformanceStatus.Running)
                {
                    Complete(PerformanceStatus.Failed);
                }
            }

            return token;
        }

        public void CancelCurrent()
        {
            if (_status == PerformanceStatus.Running)
            {
                Complete(PerformanceStatus.Cancelled);
            }
        }

        void IAbilityFrameTickable.FrameTick(float deltaTime)
        {
            if (_status != PerformanceStatus.Running)
            {
                return;
            }

            PerformanceNodeStatus nodeStatus;
            try
            {
                nodeStatus = _runningNode.Tick(_performanceContext, deltaTime);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Complete(PerformanceStatus.Failed);
                return;
            }

            switch (nodeStatus)
            {
                case PerformanceNodeStatus.Running:
                    return;
                case PerformanceNodeStatus.Succeeded:
                    Complete(PerformanceStatus.Succeeded);
                    return;
                case PerformanceNodeStatus.Failed:
                    Complete(PerformanceStatus.Failed);
                    return;
                default:
                    Debug.LogError($"[Performance] Unknown performance node status [{nodeStatus}].");
                    Complete(PerformanceStatus.Failed);
                    return;
            }
        }

        internal PerformanceStatus GetStatus(int executionId)
        {
            return executionId != 0 && executionId == _executionId ? _status : PerformanceStatus.None;
        }

        internal void Cancel(int executionId)
        {
            if (executionId == _executionId && _status == PerformanceStatus.Running)
            {
                CancelCurrent();
            }
        }

        private void Complete(PerformanceStatus status)
        {
            var node = _runningNode;
            var blockMovementToken = _blockMovementToken;
            var completingExecutionId = _executionId;
            _runningNode = null;
            _blockMovementToken = default;
            _status = status;

            try
            {
                node?.Exit(_performanceContext, status);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (_executionId == completingExecutionId)
                {
                    _status = PerformanceStatus.Failed;
                }
            }
            finally
            {
                blockMovementToken.Cancel();
            }
        }

        private void GenerateExecutionId()
        {
            unchecked
            {
                ++_executionId;
                if (_executionId == 0)
                {
                    ++_executionId;
                }
            }
        }
    }
}
