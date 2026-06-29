namespace Narrative.Interaction.Core
{
    /// <summary>
    /// The single intent an NPC reads as this run, derived from its casting at placement time (R1-R3 of
    /// the NPC Proximity Interaction requirement). There is NO authored hostility flag — intent is a
    /// function of the facts at placement: a quest on offer wins; otherwise an available fight makes the
    /// NPC hostile; otherwise it is a plain talkable villager.
    /// </summary>
    public enum NpcIntent
    {
        /// <summary>No quest to offer and no fight available — talkable via the F prompt, no marker.</summary>
        Plain = 0,

        /// <summary>The casting filled a quest slot — shows a <c>?</c>, talkable via the F prompt.</summary>
        QuestBearer = 1,

        /// <summary>No quest to offer but a fight is available — shows a <c>!</c>, auto-engages on approach.</summary>
        Hostile = 2
    }
}
