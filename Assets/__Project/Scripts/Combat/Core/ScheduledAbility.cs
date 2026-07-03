namespace Combat.Core
{
    /// <summary>
    /// Represents an ability scheduled in a unit's execution queue.
    /// Carries no direction: directional abilities fire along the unit's facing at
    /// execution time (one global facing re-points the whole queue).
    /// </summary>
    public struct ScheduledAbility
    {
        /// <summary>
        /// The ability instance to execute.
        /// </summary>
        public IAbilityInstance Ability { get; }

        /// <summary>
        /// The order in which this ability executes in the queue (0 = first).
        /// </summary>
        public int ExecutionOrder { get; }

        public ScheduledAbility(IAbilityInstance ability, int executionOrder)
        {
            Ability = ability;
            ExecutionOrder = executionOrder;
        }
    }
}
