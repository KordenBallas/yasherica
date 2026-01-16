namespace Combat.Core
{
    /// <summary>
    /// Runtime damage ability created from ScriptableObject definition.
    /// </summary>
    public class DataDrivenDamageAbility : Ability, IDamageAbility
    {
        public int Damage { get; }

        public DataDrivenDamageAbility(
            int id,
            string name,
            int cooldownDuration,
            AbilityTargetType targetType,
            int range,
            int damage)
            : base(id, name, cooldownDuration, targetType, range, AbilityEffectType.Damage)
        {
            Damage = damage;
        }
    }

    /// <summary>
    /// Runtime heal ability created from ScriptableObject definition.
    /// </summary>
    public class DataDrivenHealAbility : Ability, IHealAbility
    {
        public int HealAmount { get; }

        public DataDrivenHealAbility(
            int id,
            string name,
            int cooldownDuration,
            AbilityTargetType targetType,
            int range,
            int healAmount)
            : base(id, name, cooldownDuration, targetType, range, AbilityEffectType.Heal)
        {
            HealAmount = healAmount;
        }
    }

    /// <summary>
    /// Runtime status effect ability created from ScriptableObject definition.
    /// </summary>
    public class DataDrivenStatusEffectAbility : Ability, IStatusEffectAbility
    {
        public IStatusEffect EffectToApply { get; }
        public int EffectDuration { get; }

        public DataDrivenStatusEffectAbility(
            int id,
            string name,
            int cooldownDuration,
            AbilityTargetType targetType,
            int range,
            IStatusEffect effect,
            int effectDuration)
            : base(id, name, cooldownDuration, targetType, range, AbilityEffectType.StatusEffect)
        {
            EffectToApply = effect;
            EffectDuration = effectDuration;
        }
    }

    /// <summary>
    /// Runtime hybrid ability (damage + status effect) created from ScriptableObject definition.
    /// </summary>
    public class DataDrivenHybridAbility : Ability, IDamageAbility, IStatusEffectAbility
    {
        public int Damage { get; }
        public IStatusEffect EffectToApply { get; }
        public int EffectDuration { get; }

        public DataDrivenHybridAbility(
            int id,
            string name,
            int cooldownDuration,
            AbilityTargetType targetType,
            int range,
            int damage,
            IStatusEffect effect,
            int effectDuration)
            : base(id, name, cooldownDuration, targetType, range, AbilityEffectType.Hybrid)
        {
            Damage = damage;
            EffectToApply = effect;
            EffectDuration = effectDuration;
        }
    }
}
