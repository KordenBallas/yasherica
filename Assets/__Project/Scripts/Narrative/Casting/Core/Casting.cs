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

        public bool QuestSlotFilled => OptionalQuest != null;
        public bool CombatSlotFilled => !string.IsNullOrEmpty(OptionalEnemyId);

        /// <summary>Derived (W2-3): the combat branch is available only if the combat slot was filled.</summary>
        public bool CombatAllowed => CombatSlotFilled;

        public Casting(NpcInstance actor, DialogueData dialogue, QuestData optionalQuest, string optionalEnemyId, ContextBag context)
        {
            Actor = actor;
            Dialogue = dialogue;
            OptionalQuest = optionalQuest;
            OptionalEnemyId = optionalEnemyId;
            Context = context ?? new ContextBag();
        }
    }
}
