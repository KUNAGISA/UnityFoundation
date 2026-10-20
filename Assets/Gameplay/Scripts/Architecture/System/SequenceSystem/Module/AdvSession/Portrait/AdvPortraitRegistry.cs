using System.Collections.Generic;
using Aoiro;
using Gameplay.Common;
using UnityEngine;

namespace Gameplay.Sequence
{
    public sealed class AdvPortraitRegistry
    {
        private readonly Dictionary<int, AdvPortraitEntry> _entries = new Dictionary<int, AdvPortraitEntry>();
        private readonly Signal<AdvPortraitChange> _changed = new Signal<AdvPortraitChange>();
        private readonly AdvPortraitVisualState _defaultVisualState;

        public IReadOnlyDictionary<int, AdvPortraitEntry> Entries => _entries;
        public ISignal<AdvPortraitChange> Changed => _changed;

        public AdvPortraitRegistry()
        {
            _defaultVisualState.Reset();
        }

        public void Clear()
        {
            _changed.Clear();
            _entries.Clear();
        }

        public bool TryGet(int portraitId, out AdvPortraitEntry entry) => _entries.TryGetValue(portraitId, out entry);

        public AdvPortraitChannelToken AcquireChannel(int portraitId, AdvPortraitChannel channel)
        {
            return TryGetRegisteredPortrait(portraitId, out var portrait) ? portrait.AcquireChannel(channel) : default;
        }

        public bool IsTokenValid(in AdvPortraitChannelToken token)
        {
            return _entries.TryGetValue(token.PortraitId, out var entry) && entry.IsValid(token);
        }

        public ref readonly AdvPortraitVisualState GetVisualStateOrDefault(int portraitId)
        {
            return ref _entries.TryGetValue(portraitId, out var entry) ? ref entry.VisualState : ref _defaultVisualState;
        }

        public void SetAsset(int portraitId, string assetLocation, Vector2? position)
        {
            var portrait = GetOrAdd(portraitId);
            if (position.HasValue)
            {
                portrait.MutableVisualState.Position = position.Value;
            }

            portrait.AssetLocation = assetLocation;
            EmitChanged(portraitId, AdvPortraitChangeType.Asset);
        }

        public void SetSortingOrder(int portraitId, int order)
        {
            if (TryGetRegisteredPortrait(portraitId, out var portrait))
            {
                portrait.SortingOrder = order;
                EmitChanged(portraitId, AdvPortraitChangeType.SortingOrder);
            }
        }

        public void SetOrigin(int portraitId, Vector2 origin)
        {
            if (TryGetRegisteredPortrait(portraitId, out var portrait))
            {
                portrait.MutableVisualState.Origin = origin;
                EmitChanged(portraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetHorizontalFlip(int portraitId, bool flip)
        {
            if (TryGetRegisteredPortrait(portraitId, out var portrait))
            {
                portrait.MutableVisualState.HorizontalFlip = flip;
                EmitChanged(portraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetVerticalFlip(int portraitId, bool flip)
        {
            if (TryGetRegisteredPortrait(portraitId, out var portrait))
            {
                portrait.MutableVisualState.VerticalFlip = flip;
                EmitChanged(portraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetPosition(in AdvPortraitChannelToken token, Vector2 value)
        {
            if (TryGet(token, AdvPortraitChannel.Position, out var portrait))
            {
                portrait.MutableVisualState.Position = value;
                EmitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetOpacity(in AdvPortraitChannelToken token, float value)
        {
            if (TryGet(token, AdvPortraitChannel.Opacity, out var portrait))
            {
                portrait.MutableVisualState.Opacity = value;
                EmitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetTint(in AdvPortraitChannelToken token, ColorRGB value)
        {
            if (TryGet(token, AdvPortraitChannel.Tint, out var portrait))
            {
                portrait.MutableVisualState.Tint = value;
                EmitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetScale(in AdvPortraitChannelToken token, Vector2 value)
        {
            if (TryGet(token, AdvPortraitChannel.Scale, out var portrait))
            {
                portrait.MutableVisualState.Scale = value;
                EmitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        public void SetRotationDegrees(in AdvPortraitChannelToken token, float value)
        {
            if (TryGet(token, AdvPortraitChannel.Rotation, out var portrait))
            {
                portrait.MutableVisualState.RotationDegrees = value;
                EmitChanged(token.PortraitId, AdvPortraitChangeType.Visual);
            }
        }

        private void EmitChanged(int portraitId, AdvPortraitChangeType type) => _changed.Emit(new AdvPortraitChange(portraitId, type));

        private AdvPortraitEntry GetOrAdd(int portraitId)
        {
            if (!_entries.TryGetValue(portraitId, out var entry))
            {
                entry = new AdvPortraitEntry(portraitId);
                _entries.Add(portraitId, entry);
            }
            return entry;
        }

        private bool TryGet(in AdvPortraitChannelToken token, AdvPortraitChannel channel, out AdvPortraitEntry entry)
        {
            entry = null;
            return token.Channel == channel && _entries.TryGetValue(token.PortraitId, out entry) && entry.IsValid(token);
        }

        private bool TryGetRegisteredPortrait(int portraitId, out AdvPortraitEntry portrait)
        {
            if (_entries.TryGetValue(portraitId, out portrait))
            {
                return true;
            }
            Debug.LogError($"[Sequence] 未注册立绘 Id [{portraitId}]。");
            return false;
        }
    }
}
