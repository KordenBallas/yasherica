using System.Collections.Generic;
using Narrative.Actors.Core;

namespace Narrative.Runtime.Snapshots
{
    /// <summary>
    /// Pure bridge between <see cref="ILiveActorRegistry"/> and its serializable snapshot. An
    /// <see cref="NpcInstance"/> is fully reconstructible from its four id/string fields; per-actor
    /// state is NOT here — it lives in the fact store under <c>actor.&lt;InstanceId&gt;.*</c> and
    /// rides the fact snapshot. Restore replays <see cref="ILiveActorRegistry.Register"/> in captured
    /// (registration) order, which is idempotent on InstanceId.
    /// </summary>
    public static class ActorSnapshotMapper
    {
        public static List<NpcInstanceSnapshot> Capture(ILiveActorRegistry registry)
        {
            var snapshots = new List<NpcInstanceSnapshot>();
            if (registry == null)
            {
                return snapshots;
            }

            foreach (var actor in registry.LiveActors)
            {
                snapshots.Add(new NpcInstanceSnapshot
                {
                    InstanceId = actor.InstanceId,
                    ArchetypeId = actor.ArchetypeId,
                    ChosenDisplayName = actor.ChosenDisplayName,
                    FactionId = actor.FactionId
                });
            }

            return snapshots;
        }

        public static void Restore(IReadOnlyList<NpcInstanceSnapshot> snapshots, ILiveActorRegistry registry)
        {
            if (snapshots == null || registry == null)
            {
                return;
            }

            foreach (var snapshot in snapshots)
            {
                registry.Register(new NpcInstance(
                    snapshot.InstanceId, snapshot.ArchetypeId, snapshot.ChosenDisplayName, snapshot.FactionId));
            }
        }
    }
}
