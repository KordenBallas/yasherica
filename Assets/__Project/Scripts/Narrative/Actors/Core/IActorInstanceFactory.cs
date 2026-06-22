namespace Narrative.Actors.Core
{
    /// <summary>
    /// Mints a per-run <see cref="NpcInstance"/> from an <see cref="NpcArchetypeData"/> when an actor is
    /// placed into the world. The instance carries the run-stable identity the director/casting and the
    /// shared fact store key off (<c>actor.&lt;InstanceId&gt;.*</c>, R12), so the same instance must be
    /// reused across that actor's encounters (the caller stores it on the placed content).
    /// </summary>
    public interface IActorInstanceFactory
    {
        /// <summary>
        /// Creates a fresh instance of <paramref name="archetype"/>: a unique, deterministic instance id,
        /// a display name drawn from the archetype's pool via the seeded stream, and the archetype's
        /// faction. Returns null when <paramref name="archetype"/> is null (fail-closed).
        /// </summary>
        NpcInstance Create(NpcArchetypeData archetype);
    }
}
