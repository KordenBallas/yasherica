using Combat.Core;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using UnityEngine;

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
            IAction action = validActions[index];
            Debug.Log($"[SimpleRandomAI] Decided to perform {action.Type}.");
            return action;
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

            Debug.Log($"[SimpleRandomAI] Identified {actions.Count} valid actions.");
            
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
            int maxRange = 1; // SimpleRandomAI uses 1 hex movement range

            // Use gameState's battlefield-aware method
            var validPositions = gameState.GetValidPositionsInRange(unit.Position, maxRange);

            Debug.Log($"[SimpleRandomAI] Found {validPositions.Count} valid move positions within range {maxRange}");

            return validPositions.ToList();
        }
        
        // To be refactored
        private int CalculateDistance(HexCoordinates from, HexCoordinates to)
        {
            var dq = System.Math.Abs(from.Q - to.Q);
            var dr = System.Math.Abs(from.R - to.R);
            var ds = System.Math.Abs((from.Q + from.R) - (to.Q + to.R));
            return (dq + dr + ds) / 2;
        }
    }
}

