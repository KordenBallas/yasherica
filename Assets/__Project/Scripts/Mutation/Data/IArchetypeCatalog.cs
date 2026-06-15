using System.Collections.Generic;
using Mutation.Data.Definitions;

namespace Mutation.Data
{
    /// <summary>
    /// Lookup from archetype id to its authored definition.
    /// Used to validate artifact archetype weights and (later) to drive mutation UI.
    /// </summary>
    public interface IArchetypeCatalog
    {
        IReadOnlyList<ArchetypeDefinition> All { get; }

        bool Contains(string archetypeId);

        bool TryGet(string archetypeId, out ArchetypeDefinition definition);
    }
}
