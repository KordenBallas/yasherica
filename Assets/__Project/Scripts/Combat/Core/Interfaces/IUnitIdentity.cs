namespace Combat.Core
{
    /// <summary>
    /// Represents unit identity information.
    /// Part of interface segregation for IUnit.
    /// </summary>
    public interface IUnitIdentity
    {
        /// <summary>
        /// Unique identifier for this unit.
        /// </summary>
        int Id { get; }
        
        /// <summary>
        /// The player who owns this unit.
        /// </summary>
        IPlayer Owner { get; }
    }
}
