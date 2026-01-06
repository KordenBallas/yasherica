using Combat.Core;

namespace Combat.Core
{
    /// <summary>
    /// Example: Basic melee attack ability.
    /// Deals damage to a single enemy target.
    /// </summary>
    public class MeleeAttackAbility : Ability, IDamageAbility
    {
        public int Damage { get; }
        
        public MeleeAttackAbility(int damage = 10)
            : base(
                id: 1,
                name: "Melee Attack",
                cooldownDuration: 0, // No cooldown - can use every turn
                targetType: AbilityTargetType.Enemy,
                range: 1, // Adjacent only
                effectType: AbilityEffectType.Damage)
        {
            Damage = damage;
        }
    }
    
    /// <summary>
    /// Example: Powerful attack with cooldown.
    /// </summary>
    public class PowerAttackAbility : Ability, IDamageAbility
    {
        public int Damage { get; }
        
        public PowerAttackAbility(int damage = 25)
            : base(
                id: 2,
                name: "Power Attack",
                cooldownDuration: 2, // 3 turn cycle
                targetType: AbilityTargetType.Enemy,
                range: 1,
                effectType: AbilityEffectType.Damage)
        {
            Damage = damage;
        }
    }
    
    /// <summary>
    /// Example: Healing ability.
    /// </summary>
    public class HealAbility : Ability, IHealAbility
    {
        public int HealAmount { get; }
        
        public HealAbility(int healAmount = 15)
            : base(
                id: 3,
                name: "Heal",
                cooldownDuration: 3, // 4 turn cycle
                targetType: AbilityTargetType.Ally,
                range: 2,
                effectType: AbilityEffectType.Heal)
        {
            HealAmount = healAmount;
        }
    }
    
    /// <summary>
    /// Example: Poison strike - damage + status effect.
    /// </summary>
    public class PoisonStrikeAbility : Ability, IDamageAbility, IStatusEffectAbility
    {
        public int Damage { get; }
        public IStatusEffect EffectToApply { get; }
        public int EffectDuration { get; }
        
        public PoisonStrikeAbility(int damage = 8, int poisonDamage = 5, int duration = 3)
            : base(
                id: 4,
                name: "Poison Strike",
                cooldownDuration: 1, // 2 turn cycle
                targetType: AbilityTargetType.Enemy,
                range: 1,
                effectType: AbilityEffectType.Hybrid)
        {
            Damage = damage;
            EffectToApply = new PoisonEffect(poisonDamage, duration);
            EffectDuration = duration;
        }
    }
}

