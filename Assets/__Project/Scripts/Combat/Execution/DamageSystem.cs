using Combat.Core;

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
            // For now, return base damage
            // Future extensions can add:
            // - Armor reduction
            // - Damage type effectiveness
            // - Buffs/debuffs
            return baseDamage;
        }
    }
}

