using Narrative.Director.Core;

namespace Narrative.Actors.Core
{
    /// <summary>
    /// Default <see cref="IActorInstanceFactory"/>. Instance ids are a monotonic ordinal scoped to this
    /// factory (run-unique, deterministic, replay-stable): two placements of the same archetype get
    /// distinct instances so their per-actor facts never collide (D2). The display name is drawn from the
    /// archetype's pool via the shared seeded <see cref="IRandomSource"/>, falling back to the archetype id
    /// when the pool is empty.
    /// </summary>
    public sealed class ActorInstanceFactory : IActorInstanceFactory
    {
        private readonly IRandomSource _random;
        private int _ordinal;

        public ActorInstanceFactory(IRandomSource random)
        {
            _random = random;
        }

        public NpcInstance Create(NpcArchetypeData archetype)
        {
            if (archetype == null)
            {
                return null;
            }

            var instanceId = $"{archetype.ArchetypeId}#{++_ordinal}";
            var displayName = PickDisplayName(archetype);
            return new NpcInstance(instanceId, archetype.ArchetypeId, displayName, archetype.FactionId);
        }

        private string PickDisplayName(NpcArchetypeData archetype)
        {
            var pool = archetype.DisplayNamePool;
            if (pool == null || pool.Count == 0)
            {
                return archetype.ArchetypeId;
            }

            return pool[_random.NextInt(pool.Count)];
        }
    }
}
