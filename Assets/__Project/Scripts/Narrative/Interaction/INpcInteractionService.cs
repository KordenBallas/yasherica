using Platform;

namespace Narrative.Interaction
{
    /// <summary>
    /// Binds/unbinds a placed NPC or a spawned enemy body to the proximity system (overhead view +
    /// registry handle). Injected into <see cref="NpcContent"/> / the combat idle state so the content
    /// adapters stay free of view and registry details.
    /// </summary>
    public interface INpcInteractionService
    {
        void Bind(NpcContent npc);
        void Unbind(NpcContent npc);

        /// <summary>
        /// Registers a spawned enemy body as a hostile proximity handle (name + `!` marker + aggro
        /// radius): crossing its radius starts the platform's fight. Camp crews behind a boss are NOT
        /// bound — the boss owns the trigger.
        /// </summary>
        void BindEnemy(EnemyContent enemy, IPlatform platform, string displayName);
    }
}
