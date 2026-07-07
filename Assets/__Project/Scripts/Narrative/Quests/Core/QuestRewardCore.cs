namespace Narrative.Quests.Core
{
    /// <summary>
    /// An immutable, UnityEngine-free DECLARED quest reward (P1-5): never a literal item id. A quest
    /// promises how good (<see cref="Tier"/> — the same scale as the artifact/mutation tier glow),
    /// what kind of thing (<see cref="PayloadKind"/>), and whose currency (<see cref="BelongingId"/> —
    /// a race id for a Part-Blank, a reward-family id for an artifact). The concrete item is rolled on
    /// completion by the loot layer, deterministically under the run seed; the offer card shows tier
    /// as glow and belonging as colour while the item itself stays hidden.
    /// </summary>
    public sealed class QuestRewardCore
    {
        /// <summary>Declared potency; rides the fiction, never a kill-counter scale.</summary>
        public int Tier { get; }

        /// <summary>
        /// The reward's family: a race id when <see cref="PayloadKind"/> is a Part-Blank, a
        /// reward-family id (e.g. power vs utility) when it is an artifact. Empty = unconstrained
        /// (the roll draws from the whole pool; the card tints neutral).
        /// </summary>
        public string BelongingId { get; }

        public QuestRewardPayloadKind PayloadKind { get; }

        public QuestRewardCore(int tier, string belongingId, QuestRewardPayloadKind payloadKind)
        {
            Tier = tier < 0 ? 0 : tier;
            BelongingId = belongingId ?? string.Empty;
            PayloadKind = payloadKind;
        }
    }
}
