using System.Collections.Generic;

namespace Narrative.Actors.Core
{
    /// <summary>
    /// Default <see cref="ILiveActorRegistry"/>: a pure-C# list of minted actors kept in registration
    /// order. Mint order is seeded and deterministic, so iterating <see cref="LiveActors"/> is
    /// replay-stable (B2). Registration is idempotent on <see cref="NpcInstance.InstanceId"/> so a
    /// re-registered (e.g. recast) actor is not duplicated.
    /// </summary>
    public sealed class LiveActorRegistry : ILiveActorRegistry
    {
        private readonly List<NpcInstance> _actors = new List<NpcInstance>();

        public IReadOnlyList<NpcInstance> LiveActors => _actors;

        public void Register(NpcInstance instance)
        {
            if (instance == null)
            {
                return;
            }

            for (int i = 0; i < _actors.Count; i++)
            {
                if (string.Equals(_actors[i].InstanceId, instance.InstanceId, System.StringComparison.Ordinal))
                {
                    return;
                }
            }

            _actors.Add(instance);
        }
    }
}
