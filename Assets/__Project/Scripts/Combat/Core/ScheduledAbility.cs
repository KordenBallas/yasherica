namespace Combat.Core
{
    /// <summary>
    /// Represents an ability scheduled in a unit's execution queue.
    /// Immutable struct containing the ability, target, and execution order.
    /// </summary>
    public struct ScheduledAbility
    {
        /// <summary>
        /// The ability instance to execute.
        /// </summary>
        public IAbilityInstance Ability { get; }
        
        /// <summary>
        /// The target of the ability.
        /// </summary>
        public AbilityTarget Target { get; }
        
        /// <summary>
        /// The order in which this ability executes in the queue (0 = first).
        /// </summary>
        public int ExecutionOrder { get; }
        
        public ScheduledAbility(IAbilityInstance ability, AbilityTarget target, int executionOrder)
        {
            Ability = ability;
            Target = target;
            ExecutionOrder = executionOrder;
        }
    }
}

