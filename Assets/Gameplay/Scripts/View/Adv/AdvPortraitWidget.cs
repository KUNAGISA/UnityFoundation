using System.Collections.Generic;
using Aoiro;
using Gameplay.Asset;
using Gameplay.Sequence;
using Gameplay.UI;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Gameplay
{
    public partial class AdvPortraitWidget : UIWidget
    {
        [SerializeField, AssetsOnly, LabelText("立绘槽预制体"), FoldoutGroup("Binding Prefab")]
        private AdvPortraitSlot _portraitSlotPrefab = null;

        private readonly Dictionary<int, AdvPortraitSlot> _portraits = new Dictionary<int, AdvPortraitSlot>();
        private readonly Dictionary<int, string> _portraitLocations = new Dictionary<int, string>();
        private readonly List<AdvPortraitSlot> _portraitSortBuffer = new List<AdvPortraitSlot>();
        private AdvPortraitSession _session;
        private AssetLoader _loader;
        private SubscriptionToken _changedToken;

        public void Bind(AdvPortraitSession session, IAssetService assetService)
        {
            Unbind();
            _session = session;
            _loader = assetService.CreateLoader();
            _changedToken = session.Changed.Subscribe(OnPortraitChanged);
            RefreshPortraits();
        }

        public void Unbind()
        {
            _changedToken.Cancel();
            _changedToken = default;
            _session = null;
            foreach (var portrait in _portraits.Values)
            {
                portrait.gameObject.SetActive(false);
                portrait.ClearPortrait();
                Destroy(portrait.gameObject);
            }
            _portraits.Clear();
            _portraitLocations.Clear();
            _portraitSortBuffer.Clear();
            _loader?.Clear();
            _loader = null;
        }

        private void RefreshPortraits()
        {
            foreach (var (portraitId, entry) in _session.Entries)
            {
                RefreshPortraitAsset(portraitId, entry);
            }
            SortPortraits();
        }

        private void OnPortraitChanged(AdvPortraitChange change)
        {
            if (_session == null || !_session.TryGet(change.PortraitId, out var entry))
            {
                return;
            }

            switch (change.Type)
            {
                case AdvPortraitChangeType.Visual:
                    if (_portraits.TryGetValue(change.PortraitId, out var portrait))
                    {
                        portrait.ApplyState(entry.VisualState);
                    }
                    break;
                case AdvPortraitChangeType.Asset:
                    var created = !_portraits.ContainsKey(change.PortraitId);
                    RefreshPortraitAsset(change.PortraitId, entry);
                    if (created)
                    {
                        SortPortraits();
                    }
                    break;
                case AdvPortraitChangeType.SortingOrder:
                    if (_portraits.TryGetValue(change.PortraitId, out var slot))
                    {
                        slot.SetSortingOrder(entry.SortingOrder);
                        SortPortraits();
                    }
                    break;
            }
        }

        private void RefreshPortraitAsset(int portraitId, AdvPortraitEntry entry)
        {
            if (!_portraits.TryGetValue(portraitId, out var portraitSlot))
            {
                portraitSlot = Instantiate(_portraitSlotPrefab, transform, false);
#if UNITY_EDITOR
                portraitSlot.name = $"Portrait_{portraitId}";
#endif
                _portraits[portraitId] = portraitSlot;
            }

            if (!_portraitLocations.TryGetValue(portraitId, out var assetLocation) || assetLocation != entry.AssetLocation)
            {
                _portraitLocations[portraitId] = entry.AssetLocation;
                var prefab = _loader.LoadAssetSync<GameObject>(entry.AssetLocation);
                var instance = prefab == null ? null : Instantiate(prefab, Vector3.zero, Quaternion.identity, transform);
                if (instance == null || !instance.TryGetComponent<AdvPortrait>(out var portrait))
                {
                    if (instance != null)
                    {
                        Destroy(instance);
                    }
                    portraitSlot.ClearPortrait();
                    Debug.LogError($"[Sequence] {entry.AssetLocation} 实例化失败或者找不到组件 {typeof(AdvPortrait)}");
                }
                else
                {
                    portraitSlot.SetPortrait(portrait);
                }
            }

            portraitSlot.ApplyState(entry.VisualState);
            portraitSlot.SetSortingOrder(entry.SortingOrder);
        }

        private void SortPortraits()
        {
            foreach (var portrait in _portraits.Values)
            {
                _portraitSortBuffer.Add(portrait);
            }

            _portraitSortBuffer.Sort((a, b) =>
            {
                var result = a.SortingOrder.CompareTo(b.SortingOrder);
                return result != 0 ? result : a.transform.GetSiblingIndex().CompareTo(b.transform.GetSiblingIndex());
            });
            for (var index = 0; index < _portraitSortBuffer.Count; index++)
            {
                _portraitSortBuffer[index].transform.SetSiblingIndex(index);
            }
            _portraitSortBuffer.Clear();
        }
    }
}
