using System.Collections.Generic;

namespace Narrative.Actors.Core
{
    /// <summary>
    /// The run's set of already-minted <see cref="NpcInstance"/>s, in deterministic registration order.
    /// The windowed planner registers each actor it mints and later queries this set to *recast* an
    /// existing actor into a new story (D11 recurring-actor casting): an actor-scoped precondition is
    /// resolved as a casting query ("does a live actor carrying fact X exist?") against these instances,
    /// so per-actor facts (<c>actor.&lt;InstanceId&gt;.*</c>) accumulate into a character arc (R12).
    ///
    /// Run-scoped: bound for the lifetime of one run. The save layer repopulates it on load (deferred —
    /// see ROADMAP "Window/horizon save-state").
    /// </summary>
    public interface ILiveActorRegistry
    {
        /// <summary>The live actors in stable registration order (deterministic, replay-friendly).</summary>
        IReadOnlyList<NpcInstance> LiveActors { get; }

        /// <summary>
        /// Records a minted actor as live for the rest of the run. Ignores null and an instance whose
        /// <see cref="NpcInstance.InstanceId"/> is already registered (idempotent on id).
        /// </summary>
        void Register(NpcInstance instance);
    }
}
