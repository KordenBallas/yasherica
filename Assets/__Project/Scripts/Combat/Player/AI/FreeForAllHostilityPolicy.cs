using Combat.Core;

namespace Combat.Player.AI
{
    /// <summary>
    /// Arena offline rule: every other unit is a target (free-for-all).
    /// </summary>
    public sealed class FreeForAllHostilityPolicy : IHostilityPolicy
    {
        public bool AreHostile(IUnit a, IUnit b)
        {
            return a.Id != b.Id;
        }
    }
}
