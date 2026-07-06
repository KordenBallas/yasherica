using Combat.Core;

namespace Combat.Integration
{
    /// <summary>
    /// Fan-in for combat outcomes: combat controllers are created PER FIGHT by the factory, so a
    /// scene-scoped listener (e.g. the run lifecycle's death hook, P2-2) cannot subscribe to them
    /// directly. The factory wires every controller it creates to <see cref="Notify"/>; listeners
    /// subscribe once to <see cref="CombatEnded"/>.
    /// </summary>
    public interface ICombatOutcomeRelay
    {
        event System.Action<CombatPhase> CombatEnded;

        void Notify(CombatPhase phase);
    }
}
