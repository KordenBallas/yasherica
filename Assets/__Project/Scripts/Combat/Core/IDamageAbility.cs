namespace Combat.Core
{
    /// <summary>
    /// Interface for abilities that deal damage.
    /// </summary>
    public interface IDamageAbility : IAbility
    {
        /// <summary>
        /// Amount of damage this ability deals.
        /// </summary>
        int Damage { get; }
    }
}

