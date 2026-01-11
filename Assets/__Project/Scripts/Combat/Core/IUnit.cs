namespace Combat.Core
{
    /// <summary>
    /// Represents a combat unit on the battlefield.
    /// Composes smaller focused interfaces following Interface Segregation Principle (ISP).
    /// This allows components to depend only on the aspects of a unit they actually need.
    /// </summary>
    public interface IUnit : 
        IUnitIdentity, 
        IUnitPosition, 
        IUnitHealth, 
        IUnitCombatant, 
        IUnitActionState
    {
        // IUnit is now a composition marker interface that combines all unit aspects.
        // All properties and methods are inherited from the smaller interfaces.
        // IUnitHealth provides health properties (CurrentHP, MaxHP, IsAlive).
    }
}

