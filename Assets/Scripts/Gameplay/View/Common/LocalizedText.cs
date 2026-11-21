using Aoiro;
using TMPro;
using UnityEngine;

namespace Gameplay
{
    [DisallowMultipleComponent]
    public sealed class LocalizedText : MonoBehaviour, IGameplayController
    {
        [SerializeField]
        private TMP_Text _target = null;

        [SerializeField]
        private string _text = string.Empty;

        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? string.Empty;
                Refresh();
            }
        }

        private LocalizationSystem _localizationSystem;
        private SubscriptionToken _catalogChangedToken;

        private void Reset()
        {
            _target = GetComponent<TMP_Text>();
            _text = _target != null ? _target.text : string.Empty;
        }

        private void OnEnable()
        {
            EnsureTarget();

            if (!Application.isPlaying)
            {
                Refresh();
                return;
            }

            _localizationSystem = this.GetSystem<LocalizationSystem>();
            _catalogChangedToken = _localizationSystem.CatalogChanged.Subscribe(OnCatalogChanged);
            Refresh();
        }

        private void OnDisable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            _catalogChangedToken.Cancel();
            _catalogChangedToken = default;
            _localizationSystem = null;
        }

        private void OnValidate()
        {
            EnsureTarget();
            if (!Application.isPlaying && _target != null)
            {
                _target.text = _text;
            }
        }

        private void OnCatalogChanged(LocalizationCatalogChanged change)
        {
            if ((change.Sources & TextSourceFlags.Global) != 0)
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            EnsureTarget();
            if (_target == null)
            {
                return;
            }

            _target.text = _localizationSystem == null ? _text : _localizationSystem.Get(_text);
        }

        private void EnsureTarget()
        {
            if (_target == null)
            {
                _target = GetComponent<TMP_Text>();
            }
        }
    }
}
