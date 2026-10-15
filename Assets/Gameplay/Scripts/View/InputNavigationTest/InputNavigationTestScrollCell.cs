using FancyScrollView.Universal;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    [RequireComponent(typeof(Button))]
    public sealed class InputNavigationTestScrollCell : UniversalFancyScrollRectCell, IBindableCell<string>
    {
        private static readonly Color DisabledColor = new Color(0.25f, 0.25f, 0.25f, 1f);
        private static readonly Color SelectedColor = new Color(0.2f, 0.65f, 1f, 1f);
        private static readonly Color ActivatedColor = new Color(1f, 0.55f, 0.2f, 1f);
        private static readonly Color SelectedActivatedColor = new Color(0.15f, 0.8f, 0.55f, 1f);
        private static readonly Color NormalColor = new Color(0.18f, 0.18f, 0.22f, 1f);

        [SerializeField]
        private TMP_Text _label = null;

        [SerializeField]
        private Image _background = null;

        private Button _button;
        private bool _submitListenerRegistered;

        public void Bind(string data)
        {
            EnsureSubmitListener();
            _label.text = data;
        }

        protected override void UpdateSelectionState(bool selected, bool canSelect)
        {
            EnsureSubmitListener();
            _button.interactable = canSelect;
            var activated = Context.TryGet<ItemActivation>(out var activation) && activation.ActivatedIndex == Index;
            _background.color = ResolveBackgroundColor(selected, activated, canSelect);
        }

        private void OnDestroy()
        {
            if (_button != null && _submitListenerRegistered)
            {
                _button.onClick.RemoveListener(Submit);
            }
        }

        private void EnsureSubmitListener()
        {
            if (_button == null)
            {
                _button = GetComponent<Button>();
            }
            if (_button != null && !_submitListenerRegistered)
            {
                _button.onClick.AddListener(Submit);
                _submitListenerRegistered = true;
            }
        }

        private void Submit()
        {
            if (Context.TryGet<IItemInteraction>(out var interaction))
            {
                interaction.TrySubmitItem(Index);
            }
        }

        private static Color ResolveBackgroundColor(bool selected, bool activated, bool canSelect)
        {
            return !canSelect ? DisabledColor : selected && activated ? SelectedActivatedColor : selected ? SelectedColor : activated ? ActivatedColor : NormalColor;
        }
    }
}
