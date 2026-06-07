using System.Collections.Generic;
using Narrative.Data.Definitions;

namespace Narrative.Generation
{
    /// <summary>
    /// Runtime binding of an NPC to a story with resolved rewards.
    /// Produced by LevelNarrativeGenerator and consumed by platform content.
    /// </summary>
    public class NpcAssignment
    {
        public NpcDefinition Npc { get; }
        public StoryDefinition Story { get; }
        public IReadOnlyList<ResolvedReward> Rewards { get; }

        /// <summary>
        /// Whether this NPC has an assigned story (vs character-only).
        /// </summary>
        public bool HasStory => Story != null;

        public NpcAssignment(NpcDefinition npc, StoryDefinition story, IReadOnlyList<ResolvedReward> rewards)
        {
            Npc = npc;
            Story = story;
            Rewards = rewards ?? System.Array.Empty<ResolvedReward>();
        }

        /// <summary>
        /// Creates a character-only assignment (NPC with no story).
        /// </summary>
        public static NpcAssignment CharacterOnly(NpcDefinition npc)
        {
            return new NpcAssignment(npc, null, null);
        }
    }

    /// <summary>
    /// A reward resolved from a RewardSlot after probability roll.
    /// </summary>
    public class ResolvedReward
    {
        public RewardDefinition Definition { get; }
        public int Quantity { get; }

        public ResolvedReward(RewardDefinition definition, int quantity)
        {
            Definition = definition;
            Quantity = quantity;
        }
    }
}
