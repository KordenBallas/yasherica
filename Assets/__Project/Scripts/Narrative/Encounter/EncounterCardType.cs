namespace Narrative.Encounter
{
    /// <summary>
    /// The typed kinds of card the encounter hand can present. Presentation-layer only — it drives the
    /// card's visual treatment and which runner verb a pick routes to; it carries no game state.
    /// </summary>
    public enum EncounterCardType
    {
        /// <summary>Generic talk/reply card (reserved; the slice uses QuestOffer for Ink replies).</summary>
        Talk = 0,

        /// <summary>An Ink-authored offer/reply; picking it selects that Ink choice.</summary>
        QuestOffer = 1,

        /// <summary>The Monster verb; picking it initiates combat (Ink choice or system TriggerCombat).</summary>
        Attack = 2,

        /// <summary>Always present; picking it leaves the encounter.</summary>
        Leave = 3
    }
}
