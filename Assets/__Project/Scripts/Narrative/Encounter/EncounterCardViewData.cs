namespace Narrative.Encounter
{
    /// <summary>
    /// View-layer DTO for one card in the encounter hand. The presenter resolves the text and type, so
    /// the view never sees Core or casting types. The reward tier-glow / belonging-color fields are a
    /// deferred follow-up (gated on the crafting artifact tier model), so the MVP carries only the text
    /// and type. A quest card additionally carries the job it labels — its <see cref="QuestTitle"/> and
    /// <see cref="QuestObjective"/> (Encounter UI R9) — both null for Attack/Leave cards, which render
    /// only <see cref="Label"/>.
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

        public EncounterCardViewData(int index, string label, EncounterCardType cardType)
            : this(index, label, cardType, null, null)
        {
        }

        public EncounterCardViewData(int index, string label, EncounterCardType cardType,
            string questTitle, string questObjective)
        {
            Index = index;
            Label = label;
            CardType = cardType;
            QuestTitle = questTitle;
            QuestObjective = questObjective;
        }
    }
}
