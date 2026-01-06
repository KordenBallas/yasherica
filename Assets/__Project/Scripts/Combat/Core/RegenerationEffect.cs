namespace Combat.Core
{
    /// <summary>
    /// Regeneration effect that heals each turn.
    /// </summary>
    public class RegenerationEffect : StatusEffect
    {
        public int HealPerTurn { get; }
        
        public RegenerationEffect(int healPerTurn = 3, int duration = 5)
            : base(
                id: 1002,
                name: "Regeneration",
                type: StatusEffectType.HealOverTime,
                duration: duration,
                stackCount: 1,
                isStackable: true)
        {
            HealPerTurn = healPerTurn;
        }
        
        public override StatusEffect DecrementDuration()
        {
            return new RegenerationEffect(HealPerTurn, Duration - 1);
        }
        
        public override StatusEffect AddStack()
        {
            // Regeneration stacks increase healing
            return new RegenerationEffect(HealPerTurn + 1, Duration);
        }
    }
}

