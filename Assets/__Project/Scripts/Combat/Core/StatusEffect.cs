namespace Combat.Core
{
    /// <summary>
    /// Base implementation of a status effect.
    /// Immutable - create new instances for duration/stack changes.
    /// </summary>
    public class StatusEffect : IStatusEffect
    {
        public int Id { get; }
        public string Name { get; }
        public StatusEffectType Type { get; }
        public int Duration { get; }
        public int StackCount { get; }
        public StackRule StackRule { get; }

        public StatusEffect(
            int id,
            string name,
            StatusEffectType type,
            int duration,
            int stackCount = 1,
            StackRule stackRule = StackRule.Refresh)
        {
            Id = id;
            Name = name;
            Type = type;
            Duration = duration;
            StackCount = stackCount;
            StackRule = stackRule;
        }

        /// <summary>
        /// Creates a new instance with duration decremented by 1.
        /// </summary>
        public virtual StatusEffect DecrementDuration()
        {
            return new StatusEffect(Id, Name, Type, Duration - 1, StackCount, StackRule);
        }

        /// <summary>
        /// Creates a new instance with stack count incremented by 1.
        /// Only StackToCap effects accumulate; everything else returns itself.
        /// </summary>
        public virtual StatusEffect AddStack()
        {
            if (StackRule != StackRule.StackToCap)
                return this;

            return new StatusEffect(Id, Name, Type, Duration, StackCount + 1, StackRule);
        }

        /// <summary>
        /// Creates a new instance with the given remaining duration (used to refresh
        /// a status back to its fresh duration on re-application).
        /// </summary>
        public virtual StatusEffect WithDuration(int duration)
        {
            return new StatusEffect(Id, Name, Type, duration, StackCount, StackRule);
        }
    }
}
