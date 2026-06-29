using Platform;

namespace Narrative.Interaction
{
    /// <summary>
    /// Binds/unbinds a placed NPC to the proximity system (overhead view + registry handle). Injected into
    /// <see cref="NpcContent"/> so the content adapter stays free of view and registry details.
    /// </summary>
    public interface INpcInteractionService
    {
        void Bind(NpcContent npc);
        void Unbind(NpcContent npc);
    }
}
