namespace Combat.Core
{
    /// <summary>
    /// Defines the type of target an ability can affect.
    /// </summary>
    public enum AbilityTargetType
    {
        /// <summary>
        /// Targets an enemy unit.
        /// </summary>
        Enemy,
        
        /// <summary>
        /// Targets an ally unit.
        /// </summary>
        Ally,
        
        /// <summary>
        /// Targets self.
        /// </summary>
        Self,
        
        /// <summary>
        /// Targets a specific position on the battlefield.
        /// </summary>
        Position,
        
        /// <summary>
        /// Targets an area (multiple cells).
        /// </summary>
        Area
    }
}

