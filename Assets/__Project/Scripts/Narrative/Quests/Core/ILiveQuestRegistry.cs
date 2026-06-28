using System.Collections.Generic;

namespace Narrative.Quests.Core
{
    /// <summary>
    /// The run's set of live <see cref="QuestInstance"/>s, in deterministic registration order. The
    /// <see cref="DialogueRunner"/> registers a quest when it is first offered and, on a later platform,
    /// looks it up by id to <em>restore</em> the same in-flight instance — so a quest can be offered in one
    /// dialogue and advanced/completed in another (cross-dialogue continuity). This is the live counterpart
    /// of the progression record: <c>IRunProgressionRecord</c> holds the status flags, this holds the live
    /// instances (objective progress, reward-granted bit) that the record does not.
    ///
    /// Run-scoped: bound for the lifetime of one run. The save layer will repopulate it on load (deferred —
    /// see ROADMAP "Window/horizon save-state").
    /// </summary>
    public interface ILiveQuestRegistry
    {
        /// <summary>The live quests in stable registration order (deterministic, replay-friendly).</summary>
        IReadOnlyList<QuestInstance> LiveQuests { get; }

        /// <summary>Returns the live instance for a quest id if one has been registered this run.</summary>
        bool TryGet(string questId, out QuestInstance quest);

        /// <summary>
        /// Records an offered quest as live for the rest of the run. Ignores null and an instance whose
        /// <see cref="QuestData.QuestId"/> is already registered (idempotent on id).
        /// </summary>
        void Register(QuestInstance quest);
    }
}
