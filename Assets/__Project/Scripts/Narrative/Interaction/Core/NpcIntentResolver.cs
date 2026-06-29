using Narrative.Stories.Core;
using CastingModel = global::Narrative.Casting.Core.Casting;

namespace Narrative.Interaction.Core
{
    /// <summary>
    /// Derives an NPC's <see cref="NpcIntent"/> from its placement-time casting and the story it was cast
    /// from. The whole classification rule, kept pure C#:
    /// <list type="bullet">
    /// <item>A quest on offer always wins — the NPC is a quest-bearer even if a fight is also possible
    /// from inside the conversation.</item>
    /// <item>Otherwise the NPC is hostile (auto-engages on approach) only when its encounter <b>forces</b>
    /// a fight — a <see cref="SlotKind.Combat"/> slot marked <b>non-optional</b>. An optional combat slot
    /// means the fight is just a branch of the dialogue (e.g. the raider you can fight <i>or</i> bribe), so
    /// the NPC stays talkable.</item>
    /// <item>Otherwise it is plain.</item>
    /// </list>
    /// A null casting (the story could not be cast) reads as plain so the NPC stays a harmless fallback.
    /// </summary>
    public sealed class NpcIntentResolver
    {
        public NpcIntent Resolve(CastingModel casting, StoryTemplateData story)
        {
            if (casting == null)
            {
                return NpcIntent.Plain;
            }

            if (casting.QuestSlotFilled)
            {
                return NpcIntent.QuestBearer;
            }

            if (HasForcedCombat(story))
            {
                return NpcIntent.Hostile;
            }

            return NpcIntent.Plain;
        }

        /// <summary>
        /// True when the story has a required (non-optional) combat slot — the encounter cannot avoid the
        /// fight, so the NPC aggros on approach. A successful casting guarantees such a slot is filled
        /// (a required slot with no matching fragment aborts the cast).
        /// </summary>
        private static bool HasForcedCombat(StoryTemplateData story)
        {
            if (story == null)
            {
                return false;
            }

            for (int i = 0; i < story.Slots.Count; i++)
            {
                var slot = story.Slots[i];
                if (slot.Kind == SlotKind.Combat && !slot.Optional)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
