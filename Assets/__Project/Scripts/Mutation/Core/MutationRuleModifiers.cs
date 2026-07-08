namespace Mutation.Core
{
    /// <summary>
    /// Run-scoped mutation rule changes (heat-ascension FR8) as a neutral-by-default record, so the
    /// mutation system never references the Heat system: the Area installer projects the run's pact
    /// onto it. Both cuts floor at one — a stingier cauldron narrows choice, never removes it.
    /// </summary>
    public sealed class MutationRuleModifiers
    {
        /// <summary>Today's rules — no modifier active.</summary>
        public static readonly MutationRuleModifiers Neutral = new MutationRuleModifiers(0, 0);

        /// <summary>Unseal variant options removed from the offer (the stingy cauldron; floor 1).</summary>
        public int VariantOptionCut { get; }

        /// <summary>Sockets removed from every part blank (thinner medallions; floor 1).</summary>
        public int SocketCut { get; }

        public MutationRuleModifiers(int variantOptionCut, int socketCut)
        {
            VariantOptionCut = variantOptionCut < 0 ? 0 : variantOptionCut;
            SocketCut = socketCut < 0 ? 0 : socketCut;
        }
    }
}
