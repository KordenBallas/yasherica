namespace Narrative.Quests.Core
{
    /// <summary>
    /// Which kind of item a quest's declared reward rolls into on completion (P1-5). A quest rolls
    /// exactly one kind per reward declaration; the kind decides where the reward's belonging is read
    /// from (an artifact's function family vs a blank's race) and which container receives it.
    /// </summary>
    public enum QuestRewardPayloadKind
    {
        /// <summary>A function-only cauldron reagent (substance/property/tier, no race).</summary>
        Artifact = 0,

        /// <summary>A Part-Blank — the mutation-loop currency carrying form + a race tag + sockets.</summary>
        PartBlank = 1
    }
}
