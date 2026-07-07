namespace Narrative.Encounter
{
    /// <summary>
    /// View-layer DTO for one card in the encounter hand. The presenter resolves the text and type, so
    /// the view never sees Core or casting types. A quest card additionally carries the job it labels —
    /// its <see cref="QuestTitle"/> and <see cref="QuestObjective"/> (Encounter UI R9) — plus the reward
    /// telegraph (P1-6): the declared <see cref="RewardTier"/> (glow) and <see cref="BelongingId"/>
    /// (colour, resolved to a tint by the view via the belonging catalog). The exact rolled item is
    /// deliberately NOT here — the mystery is the point. All reward fields are inert for
    /// Attack/Leave cards, which render only <see cref="Label"/>.
    /// </summary>
    public readonly struct EncounterCardViewData
    {
        /// <summary>Position of this card in the hand; the index a pick reports back.</summary>
        public int Index { get; }

        /// <summary>Player-facing card text (the choice/verb label; the fallback when no quest job is set).</summary>
        public string Label { get; }

        /// <summary>The card kind (drives visual treatment).</summary>
        public EncounterCardType CardType { get; }

        /// <summary>Quest title shown on a quest card (null for non-quest cards).</summary>
        public string QuestTitle { get; }

        /// <summary>Quest objective/summary shown under the title on a quest card (null otherwise).</summary>
        public string QuestObjective { get; }

        /// <summary>Fuller job detail (objectives / giver) revealed by the inspect gesture; the face
        /// stays terse (null = nothing beyond the summary).</summary>
        public string QuestDetail { get; }

        /// <summary>True when the quest declares a reward the card should telegraph (mystery slot shown).</summary>
        public bool HasRewardTelegraph { get; }

        /// <summary>Declared reward tier — the mystery slot's glow intensity.</summary>
        public int RewardTier { get; }

        /// <summary>Declared belonging id (race or reward family) — the mystery slot's tint key.</summary>
        public string BelongingId { get; }

        public EncounterCardViewData(int index, string label, EncounterCardType cardType)
            : this(index, label, cardType, null, null)
        {
        }

        public EncounterCardViewData(int index, string label, EncounterCardType cardType,
            string questTitle, string questObjective)
            : this(index, label, cardType, questTitle, questObjective, false, 0, null, null)
        {
        }

        public EncounterCardViewData(int index, string label, EncounterCardType cardType,
            string questTitle, string questObjective, bool hasRewardTelegraph, int rewardTier,
            string belongingId, string questDetail = null)
        {
            Index = index;
            Label = label;
            CardType = cardType;
            QuestTitle = questTitle;
            QuestObjective = questObjective;
            QuestDetail = questDetail;
            HasRewardTelegraph = hasRewardTelegraph;
            RewardTier = rewardTier;
            BelongingId = belongingId;
        }
    }
}
