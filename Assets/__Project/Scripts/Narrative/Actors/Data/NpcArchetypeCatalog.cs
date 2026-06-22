using System.Collections.Generic;

namespace Narrative.Actors.Data
{
    /// <summary>Default <see cref="INpcArchetypeCatalog"/>: an id → SO lookup built from the authored set.</summary>
    public sealed class NpcArchetypeCatalog : INpcArchetypeCatalog
    {
        private readonly Dictionary<string, NpcArchetype> _byId = new Dictionary<string, NpcArchetype>();

        public NpcArchetypeCatalog(IEnumerable<NpcArchetype> archetypes)
        {
            if (archetypes == null)
            {
                return;
            }

            foreach (var archetype in archetypes)
            {
                if (archetype != null && !string.IsNullOrEmpty(archetype.ArchetypeId))
                {
                    _byId[archetype.ArchetypeId] = archetype;
                }
            }
        }

        public NpcArchetype Get(string archetypeId) =>
            !string.IsNullOrEmpty(archetypeId) && _byId.TryGetValue(archetypeId, out var archetype) ? archetype : null;
    }
}
