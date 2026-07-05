using Narrative.Actors.Core;
using Narrative.Dialogue.Core;
using Narrative.Quests.Core;

namespace Narrative.Casting.Core
{
    /// <summary>
    /// The recombination object (R3): associates an <see cref="NpcInstance"/> with the content it
    /// presents this run — a dialogue fragment, an optional quest, an optional combat enemy — plus the
    /// per-run <see cref="ContextBag"/>. Role lives here, not on the archetype.
    ///
    /// Hostility disambiguation (W2-3): there is NO stored hostility flag. <see cref="CombatAllowed"/>
    /// is derived from whether the combat slot was filled; the *runtime* "is hostile" state is the
    /// <c>actor.&lt;id&gt;.hostile</c> fact, written only by the dialogue/combat branch.
    /// </summary>
    public sealed class Casting
    {
        public NpcInstance Actor { get; }
        public DialogueData Dialogue { get; }
        public QuestData OptionalQuest { get; }
        public string OptionalEnemyId { get; }
        public ContextBag Context { get; }

        /// <summary>The story this casting presents and the thread that story belongs to — carried so
        /// the resolution relay can note the encounter's outcome on the run ledgers (R8). Empty on
        /// castings built outside a story (legacy/test paths).</summary>
        public string StoryId { get; }
        public string ThreadId { get; }

        public bool QuestSlotFilled => OptionalQuest != null;
        public bool CombatSlotFilled => !string.IsNullOrEmpty(OptionalEnemyId);

        /// <summary>Derived (W2-3): the combat branch is available only if the combat slot was filled.</summary>
        public bool CombatAllowed => CombatSlotFilled;

        public Casting(NpcInstance actor, DialogueData dialogue, QuestData optionalQuest, string optionalEnemyId,
            ContextBag context, string storyId = "", string threadId = "")
        {
            Actor = actor;
            Dialogue = dialogue;
            OptionalQuest = optionalQuest;
            OptionalEnemyId = optionalEnemyId;
            Context = context ?? new ContextBag();
            StoryId = storyId ?? string.Empty;
            ThreadId = threadId ?? string.Empty;
        }
    }
}
