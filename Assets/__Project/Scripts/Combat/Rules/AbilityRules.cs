using Combat.Core;
using Combat.Battlefield;
using System;

namespace Combat.Rules
{
    /// <summary>
    /// Rules for ability usage and targeting.
    /// </summary>
    public class AbilityRules
    {
        /// <summary>
        /// Checks if an ability can target a specific unit.
        /// </summary>
        public bool CanTarget(IAbility ability, IUnit caster, IUnit target, ICombatState gameState)
        {
            if (target == null || !target.IsAlive)
                return false;
            
            // Check target type
            switch (ability.TargetType)
            {
                case AbilityTargetType.Enemy:
                    if (target.Owner.Id == caster.Owner.Id)
                        return false;
                    break;
                    
                case AbilityTargetType.Ally:
                    if (target.Owner.Id != caster.Owner.Id)
                        return false;
                    break;
                    
                case AbilityTargetType.Self:
                    if (target.Id != caster.Id)
                        return false;
                    break;
            }
            
            // Check range
            int distance = CalculateDistance(caster.Position, target.Position);
            return distance <= ability.Range;
        }
        
        /// <summary>
        /// Checks if an ability can target a specific position.
        /// </summary>
        public bool CanTargetPosition(IAbility ability, IUnit caster, HexCoordinates targetPosition)
        {
            // Check range
            int distance = CalculateDistance(caster.Position, targetPosition);
            return distance <= ability.Range;
        }
        
        /// <summary>
        /// Calculates hex distance between two positions.
        /// </summary>
        private int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            var dq = Math.Abs(from.Q - to.Q);
            var dr = Math.Abs(from.R - to.R);
            var ds = Math.Abs((from.Q + from.R) - (to.Q + to.R));
            
            return (dq + dr + ds) / 2;
        }
    }
}

