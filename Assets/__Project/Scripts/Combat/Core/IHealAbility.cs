namespace Combat.Core
{
    /// <summary>
    /// Interface for abilities that heal targets.
    /// </summary>
    public interface IHealAbility : IAbility
    {
        /// <summary>
        /// Amount of health this ability restores.
        /// </summary>
        int HealAmount { get; }
    }
}

