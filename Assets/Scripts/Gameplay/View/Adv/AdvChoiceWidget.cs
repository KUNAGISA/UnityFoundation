using System;
using System.Collections.Generic;
using Aoiro;
using Gameplay.Sequence;
using Gameplay.UI;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;

namespace Gameplay
{
    public partial class AdvChoiceWidget : UIWidget
    {
        [SerializeField, AssetsOnly, LabelText("选项预制体"), FoldoutGroup("Binding Prefab")]
        private AdvChoiceItem _choiceItemPrefab = null;

        private readonly List<AdvChoiceItem> _choiceItems = new List<AdvChoiceItem>();
        private AdvChoiceSession _session;
        private SubscriptionToken _changedToken;
        private Action<AdvChoiceToken, int> _onChoiceClick;

        public Selectable DefaultSelectable { get; private set; }

        public void Bind(AdvChoiceSession session)
        {
            Unbind();
            _session = session;
            _onChoiceClick ??= OnChoiceClick;
            _changedToken = session.Changed.Subscribe(RefreshChoices);
            RefreshChoices();
        }

        public void Unbind()
        {
            _changedToken.Cancel();
            _changedToken = default;
            _session = null;
            DefaultSelectable = null;
            foreach (var choiceItem in _choiceItems)
            {
                choiceItem.gameObject.SetActive(false);
                Destroy(choiceItem.gameObject);
            }
            _choiceItems.Clear();
            gameObject.SetActive(false);
        }

        private void RefreshChoices()
        {
            var length = _session.Count;
            if (_choiceItems.Capacity < length)
            {
                _choiceItems.Capacity = length;
            }
            for (var index = _choiceItems.Count; index < length; ++index)
            {
                _choiceItems.Add(Instantiate(_choiceItemPrefab, transform, false));
            }
            for (var index = 0; index < length; ++index)
            {
                var choiceItem = _choiceItems[index];
                choiceItem.SetChoice(_session.Token, index, _session.GetText(index), _onChoiceClick);
                choiceItem.Selectable.navigation = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = index > 0 ? _choiceItems[index - 1].Selectable : null,
                    selectOnDown = index < length - 1 ? _choiceItems[index + 1].Selectable : null
                };
                choiceItem.gameObject.SetActive(true);
            }
            for (var index = length; index < _choiceItems.Count; ++index)
            {
                _choiceItems[index].gameObject.SetActive(false);
            }
            DefaultSelectable = length > 0 ? _choiceItems[0].Selectable : null;
            gameObject.SetActive(length > 0);
        }

        private void OnChoiceClick(AdvChoiceToken token, int index)
        {
            _session?.Select(token, index);
        }
    }
}
