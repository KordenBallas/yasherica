namespace Combat.Core
{
    /// <summary>
    /// Poison effect that deals damage each turn.
    /// </summary>
    public class PoisonEffect : StatusEffect
    {
        public int DamagePerTurn { get; }
        
        public PoisonEffect(int damagePerTurn = 5, int duration = 3)
            : base(
                id: 1001,
                name: "Poison",
                type: StatusEffectType.DamageOverTime,
                duration: duration,
                stackCount: 1,
                isStackable: true)
        {
            DamagePerTurn = damagePerTurn;
        }
        
        public override StatusEffect DecrementDuration()
        {
            return new PoisonEffect(DamagePerTurn, Duration - 1);
        }
        
        public override StatusEffect AddStack()
        {
            // Poison stacks increase damage
            return new PoisonEffect(DamagePerTurn + 2, Duration);
        }
    }
}

