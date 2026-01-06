using Combat.Core;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;

namespace Combat.Player
{
    /// <summary>
    /// Simple AI that picks random valid actions.
    /// Used for testing - no tactical evaluation.
    /// </summary>
    public class SimpleRandomAI : IAIDecisionMaker
    {
        private readonly System.Random _random;
        
        public SimpleRandomAI(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }
        
        public IAction DecideAction(ICombatState gameState, IUnit unit)
        {
            var validActions = GetValidActions(gameState, unit);
            
            if (validActions.Count == 0)
            {
                // No valid actions, end turn
                return new EndUnitTurnAction(unit.Owner, unit.Id);
            }
            
            // Pick random action
            int index = _random.Next(validActions.Count);
            return validActions[index];
        }
        
        private List<IAction> GetValidActions(ICombatState gameState, IUnit unit)
        {
            var actions = new List<IAction>();
            
            // Add available abilities as actions
            var availableAbilities = unit.GetAvailableAbilities();
            foreach (var abilityInstance in availableAbilities)
            {
                var ability = abilityInstance.Ability;
                
                // Find valid targets
                var validTargets = GetValidTargets(gameState, unit, ability);
                
                foreach (var target in validTargets)
                {
                    // Create schedule + execute action sequence
                    // For simplicity, AI will schedule and immediately execute
                    actions.Add(new ScheduleAbilityAction(
                        unit.Owner,
                        unit.Id,
                        ability.Id,
                        target
                    ));
                }
            }
            
            // Add move actions
            var validMovePositions = GetValidMovePositions(gameState, unit);
            foreach (var position in validMovePositions)
            {
                actions.Add(new MoveAction(unit.Owner, unit.Id, position));
            }
            
            // Always can end turn
            actions.Add(new EndUnitTurnAction(unit.Owner, unit.Id));
            
            return actions;
        }
        
        private List<AbilityTarget> GetValidTargets(ICombatState gameState, IUnit caster, IAbility ability)
        {
            var targets = new List<AbilityTarget>();
            
            switch (ability.TargetType)
            {
                case AbilityTargetType.Self:
                    targets.Add(AbilityTarget.ForSelf());
                    break;
                    
                case AbilityTargetType.Enemy:
                    // Find all enemy units in range
                    var enemies = gameState.Units.Where(u =>
                        u.Owner.Id != caster.Owner.Id &&
                        u.IsAlive &&
                        CalculateDistance(caster.Position, u.Position) <= ability.Range
                    );
                    
                    foreach (var enemy in enemies)
                    {
                        targets.Add(AbilityTarget.ForUnit(enemy.Id, AbilityTargetType.Enemy));
                    }
                    break;
                    
                case AbilityTargetType.Ally:
                    // Find all ally units in range
                    var allies = gameState.Units.Where(u =>
                        u.Owner.Id == caster.Owner.Id &&
                        u.IsAlive &&
                        u.Id != caster.Id &&
                        CalculateDistance(caster.Position, u.Position) <= ability.Range
                    );
                    
                    foreach (var ally in allies)
                    {
                        targets.Add(AbilityTarget.ForUnit(ally.Id, AbilityTargetType.Ally));
                    }
                    break;
            }
            
            return targets;
        }
        
        private List<HexCoordinates> GetValidMovePositions(ICombatState gameState, IUnit unit)
        {
            var validPositions = new List<HexCoordinates>();
            
            // Simple implementation: check positions within range
            int maxRange = 3; // Default movement range
            
            for (int q = -maxRange; q <= maxRange; q++)
            {
                for (int r = -maxRange; r <= maxRange; r++)
                {
                    if (q == 0 && r == 0)
                        continue; // Skip current position
                    
                    var pos = new HexCoordinates(unit.Position.Q + q, unit.Position.R + r);
                    
                    // Check distance
                    if (CalculateDistance(unit.Position, pos) > maxRange)
                        continue;
                    
                    // Check if occupied
                    if (gameState.GetUnitAt(pos) != null)
                        continue;
                    
                    validPositions.Add(pos);
                }
            }
            
            return validPositions;
        }
        
        private int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            var dq = System.Math.Abs(from.Q - to.Q);
            var dr = System.Math.Abs(from.R - to.R);
            var ds = System.Math.Abs((from.Q + from.R) - (to.Q + to.R));
            
            return (dq + dr + ds) / 2;
        }
    }
}

