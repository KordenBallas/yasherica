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
        public bool IsStackable { get; }
        
        public StatusEffect(
            int id,
            string name,
            StatusEffectType type,
            int duration,
            int stackCount = 1,
            bool isStackable = false)
        {
            Id = id;
            Name = name;
            Type = type;
            Duration = duration;
            StackCount = stackCount;
            IsStackable = isStackable;
        }
        
        /// <summary>
        /// Creates a new instance with duration decremented by 1.
        /// </summary>
        public virtual StatusEffect DecrementDuration()
        {
            return new StatusEffect(Id, Name, Type, Duration - 1, StackCount, IsStackable);
        }
        
        /// <summary>
        /// Creates a new instance with stack count incremented by 1.
        /// </summary>
        public virtual StatusEffect AddStack()
        {
            if (!IsStackable)
                return this;
                
            return new StatusEffect(Id, Name, Type, Duration, StackCount + 1, IsStackable);
        }
    }
}

