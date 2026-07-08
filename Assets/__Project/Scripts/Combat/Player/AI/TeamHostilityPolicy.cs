using Combat.Core;

namespace Combat.Player.AI
{
    /// <summary>
    /// PvE alliance rule: the AI side (AI-owned units) versus everyone else (human and
    /// network players are one team). Fellow enemies are allies even though each has its
    /// own AIPlayer instance.
    /// </summary>
    public sealed class TeamHostilityPolicy : IHostilityPolicy
    {
        public bool AreHostile(IUnit a, IUnit b)
        {
            return IsAiSide(a) != IsAiSide(b);
        }

        private static bool IsAiSide(IUnit unit)
        {
            return unit.Owner.Type == PlayerType.AI;
        }
    }
}
