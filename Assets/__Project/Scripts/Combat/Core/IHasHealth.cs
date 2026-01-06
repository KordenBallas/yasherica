namespace Combat.Core
{
    /// <summary>
    /// Interface for entities that have health points.
    /// </summary>
    public interface IHasHealth
    {
        /// <summary>
        /// Current health points.
        /// </summary>
        int CurrentHP { get; }
        
        /// <summary>
        /// Maximum health points.
        /// </summary>
        int MaxHP { get; }
        
        /// <summary>
        /// Indicates whether the entity is alive (CurrentHP > 0).
        /// </summary>
        bool IsAlive { get; }
    }
}

