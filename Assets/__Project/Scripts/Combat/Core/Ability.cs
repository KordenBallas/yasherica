namespace Combat.Core
{
    /// <summary>
    /// Base implementation of an ability.
    /// </summary>
    public class Ability : IAbility
    {
        public int Id { get; }
        public string Name { get; }
        public int CooldownDuration { get; }
        public AbilityTargetType TargetType { get; }
        public int Range { get; }
        public AbilityEffectType EffectType { get; }
        
        public Ability(
            int id,
            string name,
            int cooldownDuration,
            AbilityTargetType targetType,
            int range,
            AbilityEffectType effectType)
        {
            Id = id;
            Name = name;
            CooldownDuration = cooldownDuration;
            TargetType = targetType;
            Range = range;
            EffectType = effectType;
        }
    }
}

