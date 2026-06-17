namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// Rarity tier of a body part as a mutation choice. The enum's ordinal is the tier
    /// (<see cref="Common"/> = 0), consumed by the mutation scoring as a rarity weight: the
    /// scoring favours rarer parts only once enough archetype points have been accumulated.
    ///
    /// NOTE: rarity is a Mutation-layer concept hosted here on the Character layer by a deliberate,
    /// user-approved decision (M2) so a single asset authors a part's body and its mutation data.
    /// It references no UnityEngine type, so the mutation Core can map it to an int tier without
    /// taking a dependency on this layer. The layering trade-off is tracked in the docs/ROADMAP,
    /// mirroring the already-accepted PartDefinition -> Combat ability coupling.
    /// </summary>
    public enum MutationRarity
    {
        Common = 0,
        Uncommon = 1,
        Rare = 2,
        Epic = 3,
        Legendary = 4,
        Mythical = 5
    }
}
