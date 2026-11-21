using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    [DisallowMultipleComponent]
    public sealed class UIFocusScope : MonoBehaviour
    {
        [SerializeField]
        private Selectable _defaultSelectable = null;

        private Selectable _lastSelected;

        public Selectable DefaultSelectable => _defaultSelectable;

        public void SetDefaultSelectable(Selectable selectable) => _defaultSelectable = selectable;

        internal bool Contains(Selectable selectable) => selectable != null && selectable.transform.IsChildOf(transform);

        internal void Remember(Selectable selectable)
        {
            if (Contains(selectable))
            {
                _lastSelected = selectable;
            }
        }

        internal Selectable ResolveFocus(Selectable preferred = null)
        {
            if (IsSelectable(preferred) && Contains(preferred))
            {
                return preferred;
            }
            if (IsSelectable(_lastSelected) && Contains(_lastSelected))
            {
                return _lastSelected;
            }
            if (IsSelectable(_defaultSelectable) && Contains(_defaultSelectable))
            {
                return _defaultSelectable;
            }
            return null;
        }

        internal static bool IsSelectable(Selectable selectable)
        {
            return selectable != null && selectable.isActiveAndEnabled && selectable.IsInteractable() && selectable.navigation.mode != Navigation.Mode.None;
        }
    }
}
