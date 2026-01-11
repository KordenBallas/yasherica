namespace Combat.Core
{
    /// <summary>
    /// Represents unit health information.
    /// Part of interface segregation for IUnit.
    /// </summary>
    public interface IUnitHealth
    {
        /// <summary>
        /// Current hit points.
        /// </summary>
        int CurrentHP { get; }
        
        /// <summary>
        /// Maximum hit points.
        /// </summary>
        int MaxHP { get; }
        
        /// <summary>
        /// Indicates whether this unit is alive.
        /// </summary>
        bool IsAlive { get; }
    }
}
