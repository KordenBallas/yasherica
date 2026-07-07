using Combat.Core;
using Combat.Core.StatusEffects;

namespace Combat.Execution
{
    /// <summary>
    /// Concrete implementation of damage system.
    /// </summary>
    public class DamageSystem : IDamageSystem
    {
        public ICombatState ApplyDamage(ICombatState gameState, IUnit target, int amount)
        {
            if (target == null || amount <= 0)
                return gameState;
            
            var newHP = target.CurrentHP - amount;
            var updatedUnit = (target as Unit).WithHP(newHP);
            
            return (gameState as CombatState).WithUpdatedUnit(updatedUnit);
        }
        
        public ICombatState ApplyHealing(ICombatState gameState, IUnit target, int amount)
        {
            if (target == null || amount <= 0)
                return gameState;
            
            var newHP = target.CurrentHP + amount;
            var updatedUnit = (target as Unit).WithHP(newHP);
            
            return (gameState as CombatState).WithUpdatedUnit(updatedUnit);
        }
        
        public int CalculateFinalDamage(IUnit attacker, IUnit target, int baseDamage)
        {
            if (attacker == null || baseDamage <= 0)
                return baseDamage;

            // Flat signed modifier model (one model for timed statuses and part passives):
            // the attacker's OutgoingDamage deltas plus the target's IncomingDamage deltas,
            // each scaled by stack count. Integer-only on purpose — this path must stay
            // lockstep-deterministic across peers.
            int delta = SumModifiers(attacker, StatTarget.OutgoingDamage);
            if (target != null)
                delta += SumModifiers(target, StatTarget.IncomingDamage);

            if (delta == 0)
                return baseDamage;

            return System.Math.Max(0, baseDamage + delta);
        }

        private static int SumModifiers(IUnit unit, StatTarget statTarget)
        {
            int sum = 0;
            foreach (var effect in unit.StatusEffects)
            {
                if (effect is DataDrivenModifierEffect mod && mod.Target == statTarget)
                    sum += mod.Magnitude * effect.StackCount;
            }

            return sum;
        }
    }
}

