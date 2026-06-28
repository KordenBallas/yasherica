using System;
using Narrative.Encounter;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Narrative.View
{
    /// <summary>
    /// Thin adapter for a single encounter card. A quest card reads as the <b>job</b> — its title plus the
    /// objective/summary line (R9); an Attack/Leave card shows just its label. Author-marked key words
    /// (<c>[[ ]]</c>) are tinted the same way as in the spoken line (R11). Forwards its click as the card's
    /// hand index. Instantiated by <see cref="EncounterCardHandView"/>; holds no card logic. The frame tint
    /// is a placeholder grammar until the reward tier-glow / belonging-color treatment lands (gated on the
    /// crafting tier model).
    /// </summary>
    public class EncounterCardView : MonoBehaviour
    {
        [SerializeField] private Button _button;
        [Tooltip("Primary line: the quest title, or the Attack/Leave label")]
        [SerializeField] private TextMeshProUGUI _label;
        [Tooltip("Quest objective/summary line; empty (hidden) for non-quest cards")]
        [SerializeField] private TextMeshProUGUI _objective;
        [Tooltip("Optional graphic tinted by the card type (placeholder until tier glow lands)")]
        [SerializeField] private Graphic _frame;

        // Placeholder per-type tints; superseded by the tier-glow / belonging-color grammar later.
        private static readonly Color QuestOfferColor = new Color(0.85f, 0.78f, 0.45f);
        private static readonly Color AttackColor = new Color(0.75f, 0.25f, 0.25f);
        private static readonly Color LeaveColor = new Color(0.55f, 0.55f, 0.60f);
        private static readonly Color TalkColor = Color.white;

        private int _index;
        private Action<int> _onClicked;

        private void Awake()
        {
            if (_button != null)
            {
                _button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDestroy()
        {
            if (_button != null)
            {
                _button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Configure(int index, EncounterCardViewData data, string keywordHex, Action<int> onClicked)
        {
            _index = index;
            _onClicked = onClicked;

            bool isQuest = !string.IsNullOrEmpty(data.QuestTitle);
            string primary = isQuest ? data.QuestTitle : data.Label;

            if (_label != null)
            {
                _label.text = KeywordHighlightFormatter.ToRichText(primary ?? string.Empty, keywordHex);
            }

            if (_objective != null)
            {
                bool showObjective = isQuest && !string.IsNullOrEmpty(data.QuestObjective);
                _objective.text = showObjective
                    ? KeywordHighlightFormatter.ToRichText(data.QuestObjective, keywordHex)
                    : string.Empty;
                _objective.gameObject.SetActive(showObjective);
            }

            if (_frame != null)
            {
                _frame.color = ColorFor(data.CardType);
            }
        }

        private void HandleClicked()
        {
            _onClicked?.Invoke(_index);
        }

        private static Color ColorFor(EncounterCardType type)
        {
            switch (type)
            {
                case EncounterCardType.QuestOffer:
                    return QuestOfferColor;
                case EncounterCardType.Attack:
                    return AttackColor;
                case EncounterCardType.Leave:
                    return LeaveColor;
                default:
                    return TalkColor;
            }
        }
    }
}
