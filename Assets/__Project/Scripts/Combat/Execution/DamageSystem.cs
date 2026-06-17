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

            // Sum the attacker's standing Buff/Debuff modifiers (e.g. part-granted passives).
            // Debuffs are already stored as negative percentages by the StatusEffectFactory.
            // NOTE: only outgoing damage is modified here; max-HP / defence stat targets are
            // not wired yet (StatModifier has no stat-target dimension) - see ROADMAP.
            float modifier = 0f;
            foreach (var effect in attacker.StatusEffects)
            {
                if ((effect.Type == StatusEffectType.Buff || effect.Type == StatusEffectType.Debuff)
                    && effect is DataDrivenModifierEffect mod)
                {
                    modifier += mod.StatModifier * effect.StackCount;
                }
            }

            if (modifier == 0f)
                return baseDamage;

            var finalDamage = (int)System.Math.Round(baseDamage * (1f + modifier));
            return System.Math.Max(0, finalDamage);
        }
    }
}

