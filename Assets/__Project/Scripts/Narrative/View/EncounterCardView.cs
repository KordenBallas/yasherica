using System;
using Narrative.Encounter;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Narrative.View
{
    /// <summary>
    /// Thin adapter for a single encounter card. A quest card reads as the <b>job</b> — title plus the
    /// objective/summary line (R9) — and, when the quest declares a reward, an ornate treatment with a
    /// <b>mystery reward slot</b> (P1-6): glow intensity = declared tier, chip colour = belonging, the
    /// item itself a hidden "?" — never the rolled identity. Hover is the <b>inspect</b> gesture: the
    /// summary line swaps to the full job detail (objectives / giver) while the reward stays hidden.
    /// Author-marked key words (<c>[[ ]]</c>) are tinted the same way as in the spoken line (R11).
    /// Forwards its click as the card's hand index. Instantiated by <see cref="EncounterCardHandView"/>;
    /// holds no card logic — the belonging colour arrives pre-resolved from the hand view.
    /// </summary>
    public class EncounterCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button _button;
        [Tooltip("Primary line: the quest title, or the Attack/Leave label")]
        [SerializeField] private TextMeshProUGUI _label;
        [Tooltip("Quest objective/summary line; empty (hidden) for non-quest cards")]
        [SerializeField] private TextMeshProUGUI _objective;
        [Tooltip("Optional graphic tinted by the card type (the ornate-frame placeholder)")]
        [SerializeField] private Graphic _frame;

        [Header("Mystery reward slot (P1-6)")]
        [Tooltip("Root of the reward telegraph; hidden on cards without a declared reward")]
        [SerializeField] private GameObject _rewardSlot;
        [Tooltip("Glow graphic whose alpha steps with the declared reward tier")]
        [SerializeField] private Graphic _rewardGlow;
        [Tooltip("Chip graphic tinted with the belonging colour (race / reward family)")]
        [SerializeField] private Graphic _rewardTint;

        // Placeholder per-type tints; the quest card's reward slot carries the real grammar now.
        private static readonly Color QuestOfferColor = new Color(0.85f, 0.78f, 0.45f);
        private static readonly Color AttackColor = new Color(0.75f, 0.25f, 0.25f);
        private static readonly Color LeaveColor = new Color(0.55f, 0.55f, 0.60f);
        private static readonly Color TalkColor = Color.white;

        // Glow = tier grammar: a base warmth every promised reward has, stepping brighter per tier.
        private static readonly Color GlowColor = new Color(1f, 0.9f, 0.55f);
        private const float GlowBaseAlpha = 0.25f;
        private const float GlowAlphaPerTier = 0.25f;

        private int _index;
        private Action<int> _onClicked;
        private string _summaryRich;
        private string _detailRich;
        private bool _hasDetail;

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

        public void Configure(int index, EncounterCardViewData data, string keywordHex,
            Color belongingTint, Action<int> onClicked)
        {
            _index = index;
            _onClicked = onClicked;

            bool isQuest = !string.IsNullOrEmpty(data.QuestTitle);
            string primary = isQuest ? data.QuestTitle : data.Label;

            if (_label != null)
            {
                _label.text = KeywordHighlightFormatter.ToRichText(primary ?? string.Empty, keywordHex);
            }

            ConfigureJobText(data, keywordHex, isQuest);
            ConfigureRewardSlot(data, belongingTint);

            if (_frame != null)
            {
                _frame.color = ColorFor(data.CardType);
            }
        }

        private void ConfigureJobText(EncounterCardViewData data, string keywordHex, bool isQuest)
        {
            if (_objective == null)
            {
                return;
            }

            bool showObjective = isQuest && !string.IsNullOrEmpty(data.QuestObjective);
            _summaryRich = showObjective
                ? KeywordHighlightFormatter.ToRichText(data.QuestObjective, keywordHex)
                : string.Empty;
            _hasDetail = isQuest && !string.IsNullOrEmpty(data.QuestDetail);
            _detailRich = _hasDetail
                ? KeywordHighlightFormatter.ToRichText(data.QuestDetail, keywordHex)
                : _summaryRich;

            _objective.text = _summaryRich;
            _objective.gameObject.SetActive(showObjective);
        }

        private void ConfigureRewardSlot(EncounterCardViewData data, Color belongingTint)
        {
            if (_rewardSlot == null)
            {
                return;
            }

            bool telegraph = data.CardType == EncounterCardType.QuestOffer && data.HasRewardTelegraph;
            _rewardSlot.SetActive(telegraph);
            if (!telegraph)
            {
                return;
            }

            if (_rewardGlow != null)
            {
                float alpha = Mathf.Clamp01(GlowBaseAlpha + data.RewardTier * GlowAlphaPerTier);
                _rewardGlow.color = new Color(GlowColor.r, GlowColor.g, GlowColor.b, alpha);
            }

            if (_rewardTint != null)
            {
                _rewardTint.color = belongingTint;
            }
        }

        // Inspect on hover (P1-6 FR5): the face stays terse; the detail appears on demand and the
        // reward slot never reveals more than tier + belonging.
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_hasDetail && _objective != null)
            {
                _objective.text = _detailRich;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (_hasDetail && _objective != null)
            {
                _objective.text = _summaryRich;
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
