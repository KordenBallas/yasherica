using System.Collections.Generic;

namespace Narrative.Interaction
{
    /// <summary>
    /// Run-scoped set of active NPC interaction handles. NPCs register on spawn and unregister on
    /// destroy; the proximity presenter reads the set each tick.
    /// </summary>
    public interface INpcInteractionRegistry
    {
        void Register(NpcInteractionHandle handle);
        void Unregister(string id);
        bool TryGet(string id, out NpcInteractionHandle handle);
        IReadOnlyList<NpcInteractionHandle> Handles { get; }
    }
}
