using System;
using System.Collections.Generic;
using System.Threading;
using Aoiro;
using Cysharp.Threading.Tasks;
using Gameplay.Scheduler;
using UnityEngine;

namespace Gameplay
{
    public sealed class InteractionSystem : ProcedureCapacitySystem, ISceneBeforeLeave
    {
        private sealed class InteractionEntry
        {
            public int Id;
            public string DisplayText;
            public int Priority;
            public bool IsVisible;
            public IInteractionSource Source;
        }

        private sealed class CandidateComparer : IComparer<InteractionCandidate>
        {
            public int Compare(InteractionCandidate left, InteractionCandidate right)
            {
                var priority = right.Priority.CompareTo(left.Priority);
                return priority != 0 ? priority : left.Token.Id.CompareTo(right.Token.Id);
            }
        }

        public IReadOnlyList<InteractionCandidate> Candidates => _candidates;
        public InteractionToken CurrentInteraction { get; private set; }
        public InteractionExecutionStatus CurrentStatus { get; private set; }
        public bool HasRunningExecution => CurrentStatus == InteractionExecutionStatus.Running;

        private readonly Dictionary<int, InteractionEntry> _entries = new();
        private readonly List<InteractionCandidate> _candidates = new();
        private readonly CandidateComparer _candidateComparer = new();

        private FrameTickToken _frameTickToken;
        private bool _isActive;
        private bool _destroying;
        private int _nextInteractionId;
        private int _executionVersion;
        private IInteractionExecutionMonitor _executionMonitor;

        protected override void OnInitialize()
        {
        }

        protected override void OnShutdown()
        {
            _destroying = true;
            _frameTickToken.Cancel();
            _frameTickToken = default;
            _isActive = false;
            Clear(false);
            CurrentInteraction = default;
            CurrentStatus = InteractionExecutionStatus.None;
            _executionVersion = 0;
        }

        protected override void OnProcedureAttach(IProcedureContext context)
        {
            _isActive = true;
            _frameTickToken = context.AddFrameTickTask(OnProcedureFrameTick);
        }

        protected override void OnProcedureDetach(IProcedureContext context)
        {
            _frameTickToken.Cancel();
            _frameTickToken = default;
            _isActive = false;
            Clear();
        }

        public InteractionToken Register(IInteractionSource source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (!_isActive)
            {
                Debug.LogWarning("[Interaction] Can not register an interaction source while the system is inactive.");
                return InteractionToken.None;
            }

            var interactionId = NextId(ref _nextInteractionId, "interaction");
            var entry = new InteractionEntry
            {
                Id = interactionId,
                Source = source,
            };
            _entries.Add(interactionId, entry);
            RefreshEntry(entry);
            return new InteractionToken(interactionId);
        }

        public bool Refresh(InteractionToken interaction)
        {
            return interaction.IsValid && _entries.TryGetValue(interaction.Id, out var entry) && RefreshEntry(entry);
        }

        public void Unregister(InteractionToken interaction)
        {
            if (!interaction.IsValid || !_entries.Remove(interaction.Id))
            {
                return;
            }

            if (HasRunningExecution && CurrentInteraction == interaction)
            {
                CancelCurrent();
            }

            RebuildCandidateList();
            NotifyCandidatesChanged();
        }

        public bool TryExecute(InteractionToken interaction)
        {
            if (!_isActive || !interaction.IsValid || HasRunningExecution || !_entries.TryGetValue(interaction.Id, out var entry) || !entry.IsVisible)
            {
                return false;
            }

            var canInteract = false;
            try
            {
                canInteract = entry.Source.CanInteract();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            if (!canInteract)
            {
                HideEntry(entry);
                return false;
            }

            _executionVersion = NextId(ref _executionVersion, "interaction execution");
            var startedVersion = _executionVersion;
            CurrentInteraction = interaction;
            CurrentStatus = InteractionExecutionStatus.Running;
            _executionMonitor = null;

            InteractionResult result;
            try
            {
                result = entry.Source.Interact();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (_executionVersion == startedVersion && HasRunningExecution)
                {
                    CompleteExecution(InteractionExecutionStatus.Failed);
                }
                return true;
            }

            if (_executionVersion != startedVersion || !HasRunningExecution)
            {
                return true;
            }

            switch (result.Status)
            {
                case InteractionExecutionStatus.Running when result.Monitor != null:
                    _executionMonitor = result.Monitor;
                    PollExecution(true);
                    return true;
                case InteractionExecutionStatus.Succeeded:
                case InteractionExecutionStatus.Failed:
                    CompleteExecution(result.Status);
                    return true;
                default:
                    Debug.LogError($"[Interaction] Interaction [{interaction}] returned an invalid start result [{result.Status}].");
                    CompleteExecution(InteractionExecutionStatus.Failed);
                    return true;
            }
        }

        public void CancelCurrent()
        {
            if (!HasRunningExecution)
            {
                return;
            }

            var monitor = _executionMonitor;
            _executionMonitor = null;
            CurrentStatus = InteractionExecutionStatus.Cancelled;
            NotifyExecutionChanged();
            try
            {
                monitor?.Cancel();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        UniTask ISceneBeforeLeave.BeforeLeaveSceneAsync(CancellationToken cancellationToken)
        {
            Clear();
            return UniTask.CompletedTask;
        }

        private bool RefreshEntry(InteractionEntry entry)
        {
            var success = TryReadSource(entry.Source, out var displayText, out var priority, out var canInteract);
            entry.DisplayText = displayText;
            entry.Priority = priority;
            entry.IsVisible = success && canInteract;

            var interaction = new InteractionToken(entry.Id);
            if (!entry.IsVisible && HasRunningExecution && CurrentInteraction == interaction)
            {
                CancelCurrent();
            }

            RebuildCandidateList();
            NotifyCandidatesChanged();
            return success;
        }

        private void HideEntry(InteractionEntry entry)
        {
            if (!entry.IsVisible)
            {
                return;
            }

            entry.IsVisible = false;
            var interaction = new InteractionToken(entry.Id);
            if (HasRunningExecution && CurrentInteraction == interaction)
            {
                CancelCurrent();
            }
            RebuildCandidateList();
            NotifyCandidatesChanged();
        }

        private bool TryReadSource(IInteractionSource source, out string displayText, out int priority, out bool canInteract)
        {
            displayText = string.Empty;
            priority = 0;
            canInteract = false;
            try
            {
                displayText = source.DisplayText ?? string.Empty;
                priority = source.Priority;
                canInteract = source.CanInteract();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return false;
            }
        }

        private void OnProcedureFrameTick(in FrameTime time)
        {
            PollExecution(false);
        }

        private void PollExecution(bool notifyRunning)
        {
            if (!HasRunningExecution || _executionMonitor == null)
            {
                return;
            }

            var polledVersion = _executionVersion;
            InteractionExecutionStatus status;
            try
            {
                status = _executionMonitor.Status;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (_executionVersion == polledVersion && HasRunningExecution)
                {
                    CompleteExecution(InteractionExecutionStatus.Failed);
                }
                return;
            }

            if (_executionVersion != polledVersion || !HasRunningExecution)
            {
                return;
            }

            switch (status)
            {
                case InteractionExecutionStatus.Running:
                    if (notifyRunning)
                    {
                        NotifyExecutionChanged();
                    }
                    return;
                case InteractionExecutionStatus.Succeeded:
                case InteractionExecutionStatus.Failed:
                case InteractionExecutionStatus.Cancelled:
                    CompleteExecution(status);
                    return;
                default:
                    Debug.LogError($"[Interaction] Interaction monitor returned an invalid status [{status}].");
                    CompleteExecution(InteractionExecutionStatus.Failed);
                    return;
            }
        }

        private void CompleteExecution(InteractionExecutionStatus status)
        {
            _executionMonitor = null;
            CurrentStatus = status;
            NotifyExecutionChanged();
        }

        private void NotifyExecutionChanged()
        {
            if (_destroying || !CurrentInteraction.IsValid)
            {
                return;
            }
            this.PublishEvent(new InteractionExecutionChangedEvent(CurrentInteraction, CurrentStatus));
        }

        private void NotifyCandidatesChanged()
        {
            if (!_destroying)
            {
                this.PublishEvent(new InteractionCandidatesChangedEvent());
            }
        }

        private void RebuildCandidateList()
        {
            _candidates.Clear();
            foreach (var entry in _entries.Values)
            {
                if (entry.IsVisible)
                {
                    _candidates.Add(new InteractionCandidate(new InteractionToken(entry.Id), entry.DisplayText, entry.Priority));
                }
            }
            _candidates.Sort(_candidateComparer);
        }

        private void Clear(bool notify = true)
        {
            CancelCurrent();
            var hadEntries = _entries.Count > 0;
            _entries.Clear();
            _candidates.Clear();
            if (notify && hadEntries)
            {
                NotifyCandidatesChanged();
            }
        }

        private static int NextId(ref int value, string name)
        {
            if (value == int.MaxValue)
            {
                throw new InvalidOperationException($"The {name} id space has been exhausted.");
            }
            return ++value;
        }
    }
}
