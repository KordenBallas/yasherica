namespace Combat.Core
{
    /// <summary>
    /// Stun effect that prevents the unit from acting.
    /// </summary>
    public class StunEffect : StatusEffect
    {
        public StunEffect(int duration = 1)
            : base(
                id: 1003,
                name: "Stun",
                type: StatusEffectType.Control,
                duration: duration,
                stackCount: 1,
                isStackable: false)
        {
        }
        
        public override StatusEffect DecrementDuration()
        {
            return new StunEffect(Duration - 1);
        }
    }
}

