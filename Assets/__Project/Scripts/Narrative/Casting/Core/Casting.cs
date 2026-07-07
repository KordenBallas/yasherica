using System;
using System.Collections.Generic;
using Narrative.Actors.Core;
using Narrative.Dialogue.Core;
using Narrative.Quests.Core;

namespace Narrative.Casting.Core
{
    /// <summary>
    /// The recombination object (R3): associates an <see cref="NpcInstance"/> with the content it
    /// presents this run — a dialogue fragment, optional quests, an optional combat enemy — plus the
    /// per-run <see cref="ContextBag"/>. Role lives here, not on the archetype.
    ///
    /// Several offers (P1-9): a story may fill SEVERAL quest slots — several resolutions of the same
    /// trouble shown together in the hand. <see cref="Quests"/> holds them in slot order;
    /// <see cref="OptionalQuest"/> stays the first for the single-offer paths, and
    /// <see cref="QuestByTag"/> resolves the <c>offer-quest: &lt;tag&gt;</c> vocabulary the Ink
    /// choices/branches use.
    ///
    /// Hostility disambiguation (W2-3): there is NO stored hostility flag. <see cref="CombatAllowed"/>
    /// is derived from whether the combat slot was filled; the *runtime* "is hostile" state is the
    /// <c>actor.&lt;id&gt;.hostile</c> fact, written only by the dialogue/combat branch.
    /// </summary>
    public sealed class Casting
    {
        public NpcInstance Actor { get; }
        public DialogueData Dialogue { get; }
        public string OptionalEnemyId { get; }
        public ContextBag Context { get; }

        /// <summary>Every quest this casting can offer, in story slot order (empty when none).</summary>
        public IReadOnlyList<QuestData> Quests { get; }

        /// <summary>The story this casting presents and the thread that story belongs to — carried so
        /// the resolution relay can note the encounter's outcome on the run ledgers (R8). Empty on
        /// castings built outside a story (legacy/test paths).</summary>
        public string StoryId { get; }
        public string ThreadId { get; }

        /// <summary>The single/first offered quest (the pre-P1-9 surface; null when none).</summary>
        public QuestData OptionalQuest => Quests.Count > 0 ? Quests[0] : null;

        public bool QuestSlotFilled => Quests.Count > 0;
        public bool CombatSlotFilled => !string.IsNullOrEmpty(OptionalEnemyId);

        /// <summary>Derived (W2-3): the combat branch is available only if the combat slot was filled.</summary>
        public bool CombatAllowed => CombatSlotFilled;

        public Casting(NpcInstance actor, DialogueData dialogue, QuestData optionalQuest, string optionalEnemyId,
            ContextBag context, string storyId = "", string threadId = "")
            : this(actor, dialogue,
                optionalQuest != null ? new[] { optionalQuest } : Array.Empty<QuestData>(),
                optionalEnemyId, context, storyId, threadId)
        {
        }

        /// <summary>Builds a casting carrying several quest offers (P1-9). A factory rather than a
        /// constructor overload so a null single quest stays unambiguous at legacy call sites.</summary>
        public static Casting WithQuests(NpcInstance actor, DialogueData dialogue,
            IReadOnlyList<QuestData> quests, string optionalEnemyId, ContextBag context,
            string storyId = "", string threadId = "")
        {
            return new Casting(actor, dialogue, quests, optionalEnemyId, context, storyId, threadId);
        }

        private Casting(NpcInstance actor, DialogueData dialogue, IReadOnlyList<QuestData> quests,
            string optionalEnemyId, ContextBag context, string storyId, string threadId)
        {
            Actor = actor;
            Dialogue = dialogue;
            Quests = quests ?? Array.Empty<QuestData>();
            OptionalEnemyId = optionalEnemyId;
            Context = context ?? new ContextBag();
            StoryId = storyId ?? string.Empty;
            ThreadId = threadId ?? string.Empty;
        }

        /// <summary>
        /// The offered quest carrying the given quest tag (the <c>offer-quest:</c> argument), or the
        /// single/first quest when the tag is empty (the legacy single-offer authoring), or null.
        /// </summary>
        public QuestData QuestByTag(string questTag)
        {
            if (string.IsNullOrEmpty(questTag))
            {
                return OptionalQuest;
            }

            for (int i = 0; i < Quests.Count; i++)
            {
                var tags = Quests[i].Tags;
                for (int t = 0; t < tags.Count; t++)
                {
                    if (string.Equals(tags[t], questTag, StringComparison.Ordinal))
                    {
                        return Quests[i];
                    }
                }
            }

            return null;
        }
    }
}
