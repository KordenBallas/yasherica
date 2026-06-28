using Narrative.Facts.Core;

namespace Narrative.Dialogue.Core
{
    /// <summary>The kind of game-system bridge an Ink tag expresses (R4).</summary>
    public enum DialogueTagKind
    {
        /// <summary>Display only: <c>speaker: &lt;name&gt;</c>.</summary>
        Speaker = 0,

        /// <summary>Set a fact at play-time: <c>fact: &lt;ns&gt;.&lt;subject?&gt;.&lt;key&gt; &lt;op&gt; &lt;value&gt;</c>.</summary>
        Fact = 1,

        /// <summary>Offer the casting's quest: <c>offer-quest: &lt;slotTag|questId&gt;</c>.</summary>
        OfferQuest = 2,

        /// <summary>Start the casting's combat (suspends): <c>start-combat: &lt;slotTag&gt;</c>.</summary>
        StartCombat = 3,

        /// <summary>Terminal outcome: <c>outcome: &lt;type&gt;</c>.</summary>
        Outcome = 4,

        /// <summary>
        /// Advance one of the active quest's objectives: <c>advance-objective: &lt;objectiveId&gt; [amount]</c>.
        /// Tracks progress only; it never completes the quest (completion is the explicit
        /// <see cref="CompleteQuest"/> signal).
        /// </summary>
        AdvanceObjective = 5,

        /// <summary>Complete the active quest: <c>complete-quest: [questId]</c> (argument optional, for author validation).</summary>
        CompleteQuest = 6,

        /// <summary>Fail the active quest: <c>fail-quest: [questId]</c> (argument optional, for author validation).</summary>
        FailQuest = 7,

        /// <summary>Not a recognized bridge tag (ignored).</summary>
        Unknown = 8
    }

    /// <summary>
    /// A parsed Ink tag. For <see cref="DialogueTagKind.Fact"/> it carries a fully-typed
    /// <see cref="FactEffectCore"/>; for the others, the raw argument string.
    /// </summary>
    public sealed class DialogueTag
    {
        public DialogueTagKind Kind { get; }
        public string Argument { get; }
        public FactEffectCore Effect { get; }

        private DialogueTag(DialogueTagKind kind, string argument, FactEffectCore effect)
        {
            Kind = kind;
            Argument = argument ?? string.Empty;
            Effect = effect;
        }

        public static DialogueTag Of(DialogueTagKind kind, string argument) => new DialogueTag(kind, argument, null);
        public static DialogueTag OfFact(FactEffectCore effect) => new DialogueTag(DialogueTagKind.Fact, string.Empty, effect);
    }
}
