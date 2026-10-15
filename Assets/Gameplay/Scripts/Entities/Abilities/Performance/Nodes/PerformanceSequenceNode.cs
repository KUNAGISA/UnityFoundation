using System;

namespace Gameplay
{
    public sealed class PerformanceSequenceNode : IPerformanceNode
    {
        private readonly IPerformanceNode[] _nodes;
        private int _activeIndex;
        private bool _hasActiveNode;

        public PerformanceSequenceNode(params IPerformanceNode[] nodes)
        {
            if (nodes == null)
            {
                throw new ArgumentNullException(nameof(nodes));
            }

            if (nodes.Length == 0)
            {
                throw new ArgumentException("Performance sequence requires at least one node.", nameof(nodes));
            }

            for (var index = 0; index < nodes.Length; ++index)
            {
                if (nodes[index] == null)
                {
                    throw new ArgumentException($"Performance sequence node [{index}] is null.", nameof(nodes));
                }

                for (var otherIndex = 0; otherIndex < index; ++otherIndex)
                {
                    if (ReferenceEquals(nodes[index], nodes[otherIndex]))
                    {
                        throw new ArgumentException($"Performance sequence node [{index}] is duplicated.", nameof(nodes));
                    }
                }
            }

            _nodes = new IPerformanceNode[nodes.Length];
            Array.Copy(nodes, _nodes, nodes.Length);
        }

        void IPerformanceNode.Enter(in PerformanceContext context)
        {
            _activeIndex = 0;
            EnterActiveNode(context);
        }

        PerformanceNodeStatus IPerformanceNode.Tick(in PerformanceContext context, float deltaTime)
        {
            while (_hasActiveNode)
            {
                var node = _nodes[_activeIndex];
                var status = node.Tick(context, deltaTime);
                if (status == PerformanceNodeStatus.Running)
                {
                    return PerformanceNodeStatus.Running;
                }

                _hasActiveNode = false;
                node.Exit(context, status == PerformanceNodeStatus.Succeeded ? PerformanceStatus.Succeeded : PerformanceStatus.Failed);

                if (status == PerformanceNodeStatus.Failed)
                {
                    return PerformanceNodeStatus.Failed;
                }

                if (status != PerformanceNodeStatus.Succeeded)
                {
                    throw new InvalidOperationException($"Unknown performance node status [{status}].");
                }

                ++_activeIndex;
                if (_activeIndex >= _nodes.Length)
                {
                    return PerformanceNodeStatus.Succeeded;
                }

                EnterActiveNode(context);
            }

            throw new InvalidOperationException("Performance sequence has no active node.");
        }

        void IPerformanceNode.Exit(in PerformanceContext context, PerformanceStatus status)
        {
            if (!_hasActiveNode)
            {
                return;
            }

            _hasActiveNode = false;
            _nodes[_activeIndex].Exit(context, status);
        }

        private void EnterActiveNode(in PerformanceContext context)
        {
            _hasActiveNode = true;
            _nodes[_activeIndex].Enter(context);
        }
    }
}
