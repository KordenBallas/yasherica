using Combat.Core;

namespace Combat.Player.AI
{
    /// <summary>
    /// Decides which units the AI treats as targets. Injected per game mode because the
    /// per-enemy AIPlayer instances make Owner.Id comparisons meaningless for alliance:
    /// PvE is team-based (all AI-owned units are one side), Arena offline is free-for-all.
    /// </summary>
    public interface IHostilityPolicy
    {
        bool AreHostile(IUnit a, IUnit b);
    }
}
