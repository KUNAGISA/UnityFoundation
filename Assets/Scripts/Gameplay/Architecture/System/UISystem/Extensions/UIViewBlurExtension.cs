using System;
using System.Collections.Generic;
using DG.Tweening;
using Gameplay.Rendering;
using Gameplay.UI;
using UnityEngine;

namespace Gameplay
{
    internal sealed class UIViewBlurExtension : UIViewExtension
    {
        private sealed class ViewRecord
        {
            public UIViewToken Token;
            public UILayerId LayerId;
            public int SortingOrder;
            public bool RequestsBlur;
            public bool IsVisible;
        }

        private readonly Dictionary<UIViewToken, ViewRecord> _records = new();
        private readonly List<ViewRecord> _orderedRecords = new();
        private readonly UIRoot _root;
        private readonly DualKawaseBlurRendererFeature _blurFeature;
        private Tween _transition;
        private bool _hasActiveBoundary;

        internal UIViewBlurExtension(UIRoot root)
        {
            _root = root ?? throw new ArgumentNullException(nameof(root));
            _blurFeature = root.BlurFeature != null ? root.BlurFeature : throw new InvalidOperationException("UI root does not reference a Kawase blur renderer feature.");
            _blurFeature.Intensity = 0f;
            SetRenderingActive(false);
        }

        protected override void OnBeforeShow(in UIViewExtensionContext context)
        {
            if (!_records.TryGetValue(context.Token, out var record))
            {
                if (!_root.TryGetLayer(context.LayerId, out var layer))
                {
                    throw new InvalidOperationException($"UI layer [{context.LayerId}] is not configured by the UI root.");
                }

                record = new ViewRecord
                {
                    Token = context.Token,
                    LayerId = context.LayerId,
                    SortingOrder = layer.SortingOrder
                };
                _records.Add(record.Token, record);
                _orderedRecords.Add(record);
            }

            record.RequestsBlur = context.HasOption(UIViewOptionFlags.Blur);
            SetVisibility(in context, true);
        }

        protected override void OnBeforeHide(in UIViewExtensionContext context)
        {
            SetVisibility(in context, false);
        }

        private void SetVisibility(in UIViewExtensionContext context, bool isVisible)
        {
            if (_records.TryGetValue(context.Token, out var record))
            {
                record.IsVisible = isVisible;
                RefreshLayout(context.Host);
            }
        }

        protected override void OnReleased(in UIViewExtensionContext context)
        {
            if (!_records.Remove(context.Token, out var record))
            {
                return;
            }

            _orderedRecords.Remove(record);
            RefreshLayout(context.Host);
        }

        protected override void OnShutdown(UIViewHost host)
        {
            _records.Clear();
            _orderedRecords.Clear();
            StopTransitionAndDisable();
        }

        private void RefreshLayout(UIViewHost host)
        {
            if (host.IsShuttingDown || _root == null || _blurFeature == null)
            {
                StopTransitionAndDisable();
                return;
            }

            var boundary = FindBoundary();
            for (var index = 0; index < _orderedRecords.Count; index++)
            {
                var record = _orderedRecords[index];
                var target = boundary != null && CompareRenderOrder(record, boundary) >= 0 ? UILayerRoot.Secondary : UILayerRoot.Primary;
                if (!host.SupportsRoot(record.LayerId, target))
                {
                    StopTransitionAndDisable();
                    throw new InvalidOperationException($"UI layer [{record.LayerId}] does not support root [{target}].");
                }

                host.TrySetRoot(record.Token, target);
            }

            SetBoundaryActive(boundary != null);
        }

        private ViewRecord FindBoundary()
        {
            ViewRecord boundary = null;
            for (var index = 0; index < _orderedRecords.Count; index++)
            {
                var record = _orderedRecords[index];
                if (!record.RequestsBlur || !record.IsVisible)
                {
                    continue;
                }

                if (boundary == null || CompareRenderOrder(record, boundary) > 0)
                {
                    boundary = record;
                }
            }

            return boundary;
        }

        private void SetRenderingActive(bool isActive)
        {
            if (_blurFeature != null)
            {
                _blurFeature.SetActive(isActive);
            }

            if (_root != null && _root.ForegroundCamera != null)
            {
                _root.ForegroundCamera.enabled = isActive;
            }
        }

        private void SetBoundaryActive(bool isActive)
        {
            if (_hasActiveBoundary == isActive)
            {
                return;
            }

            _hasActiveBoundary = isActive;
            _transition?.Kill();
            _transition = null;

            var targetIntensity = isActive ? 1f : 0f;
            if (isActive)
            {
                SetRenderingActive(true);
            }

            var duration = _root.BlurTransitionDuration * Mathf.Abs(targetIntensity - _blurFeature.Intensity);
            if (duration <= 0f)
            {
                _blurFeature.Intensity = targetIntensity;
                CompleteTransition();
                return;
            }

            _transition = DOTween.To(GetIntensity, SetIntensity, targetIntensity, duration)
                .SetEase(_root.BlurTransitionEase)
                .SetUpdate(true)
                .OnComplete(CompleteTransition);
        }

        private float GetIntensity() => _blurFeature != null ? _blurFeature.Intensity : 0f;

        private void SetIntensity(float intensity)
        {
            if (_blurFeature != null)
            {
                _blurFeature.Intensity = intensity;
            }
        }

        private void CompleteTransition()
        {
            _transition = null;
            if (!_hasActiveBoundary)
            {
                SetRenderingActive(false);
            }
        }

        private void StopTransitionAndDisable()
        {
            _hasActiveBoundary = false;
            _transition?.Kill();
            _transition = null;
            if (_blurFeature != null)
            {
                _blurFeature.Intensity = 0f;
            }

            SetRenderingActive(false);
        }

        private static int CompareRenderOrder(ViewRecord left, ViewRecord right)
        {
            var layerComparison = left.SortingOrder.CompareTo(right.SortingOrder);
            return layerComparison != 0 ? layerComparison : left.Token.ViewId.CompareTo(right.Token.ViewId);
        }
    }
}
