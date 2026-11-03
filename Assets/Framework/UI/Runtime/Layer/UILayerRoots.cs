using System;
using UnityEngine;

namespace Gameplay.UI
{
    public readonly struct UILayerRoots
    {
        public Transform Primary { get; }
        public Transform Secondary { get; }
        public bool HasSecondary => Secondary != null;

        /// <summary>创建仅包含主 Root 的层配置，主 Root 不得为空。</summary>
        public UILayerRoots(Transform primary)
        {
            Primary = primary != null ? primary : throw new ArgumentNullException(nameof(primary));
            Secondary = null;
        }

        /// <summary>创建包含主、副 Root 的层配置；两个 Root 都不得为空且必须不同。</summary>
        public UILayerRoots(Transform primary, Transform secondary)
        {
            Primary = primary != null ? primary : throw new ArgumentNullException(nameof(primary));
            Secondary = secondary != null ? secondary : throw new ArgumentNullException(nameof(secondary));
            if (Primary == Secondary)
            {
                throw new ArgumentException("UI layer primary and secondary roots must be distinct.", nameof(secondary));
            }
        }

        internal bool TryGetRoot(UILayerRoot root, out Transform transform)
        {
            transform = root switch
            {
                UILayerRoot.Primary => Primary,
                UILayerRoot.Secondary => Secondary,
                _ => null
            };
            return transform != null;
        }
    }
}
