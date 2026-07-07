using Combat.Core.StatusEffects;

namespace Combat.Core
{
    /// <summary>
    /// Melee attack: line of length 1 (adjacent cell), damage.
    /// </summary>
    public class MeleeAttackAbility : Ability, IDamageAbility
    {
        public int Damage { get; }

        public MeleeAttackAbility(int damage = 10)
            : base(
                id: 1,
                name: "Melee Attack",
                cooldownDuration: 0,
                shape: AbilityShapeData.ForLine(1),
                effectType: AbilityEffectType.Damage)
        {
            Damage = damage;
        }
    }

    /// <summary>
    /// Power attack: line of length 2, high damage with cooldown.
    /// </summary>
    public class PowerAttackAbility : Ability, IDamageAbility
    {
        public int Damage { get; }

        public PowerAttackAbility(int damage = 25)
            : base(
                id: 2,
                name: "Power Attack",
                cooldownDuration: 2,
                shape: AbilityShapeData.ForLine(2),
                effectType: AbilityEffectType.Damage)
        {
            Damage = damage;
        }
    }

    /// <summary>
    /// Heal: ring of radius 1 (adjacent cells), healing.
    /// </summary>
    public class HealAbility : Ability, IHealAbility
    {
        public int HealAmount { get; }

        public HealAbility(int healAmount = 15)
            : base(
                id: 3,
                name: "Heal",
                cooldownDuration: 3,
                shape: AbilityShapeData.ForRing(1),
                effectType: AbilityEffectType.Heal)
        {
            HealAmount = healAmount;
        }
    }

    /// <summary>
    /// Poison strike: line of length 1, damage + poison effect.
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
                cooldownDuration: 1,
                shape: AbilityShapeData.ForLine(1),
                effectType: AbilityEffectType.Hybrid)
        {
            Damage = damage;
            EffectToApply = new DataDrivenDamageOverTimeEffect(
                id: 1002,
                name: "Poison",
                duration: duration,
                stackCount: 1,
                stackRule: StackRule.Refresh,
                maxStacks: 0,
                triggerType: StatusEffectTriggerType.TurnEnd,
                damagePerTrigger: poisonDamage,
                damagePerStack: 0);
            EffectDuration = duration;
        }
    }
}
