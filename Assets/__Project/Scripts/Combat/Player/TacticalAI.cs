using Combat.Core;
using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;

namespace Combat.Player
{
    /// <summary>
    /// Advanced tactical AI that evaluates damage, positioning, and threat.
    /// Makes strategic decisions based on unit HP, damage potential, and positioning.
    /// </summary>
    public class TacticalAI : IAIDecisionMaker
    {
        private readonly System.Random _random;
        
        public TacticalAI(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }
        
        public IAction DecideAction(ICombatState gameState, IUnit unit)
        {
            // Evaluate all possible actions and pick the best one
            var scoredActions = EvaluateAllActions(gameState, unit);
            
            if (scoredActions.Count == 0)
            {
                return new EndUnitTurnAction(unit.Owner, unit.Id);
            }
            
            // Pick the action with the highest score
            var bestAction = scoredActions.OrderByDescending(sa => sa.Score).First();
            return bestAction.Action;
        }
        
        private List<ScoredAction> EvaluateAllActions(ICombatState gameState, IUnit unit)
        {
            var scoredActions = new List<ScoredAction>();
            
            // Evaluate abilities
            var availableAbilities = unit.GetAvailableAbilities();
            foreach (var abilityInstance in availableAbilities)
            {
                var ability = abilityInstance.Ability;
                var validTargets = GetValidTargetsForAbility(gameState, unit, ability);
                
                foreach (var target in validTargets)
                {
                    var score = EvaluateAbilityAction(gameState, unit, ability, target);
                    scoredActions.Add(new ScoredAction(
                        new ScheduleAbilityAction(unit.Owner, unit.Id, ability.Id, target),
                        score
                    ));
                }
            }
            
            // Evaluate movement
            var moveActions = EvaluateMovementActions(gameState, unit);
            scoredActions.AddRange(moveActions);
            
            // Always can end turn (low score)
            scoredActions.Add(new ScoredAction(
                new EndUnitTurnAction(unit.Owner, unit.Id),
                10 // Base score
            ));
            
            return scoredActions;
        }
        
        private float EvaluateAbilityAction(ICombatState gameState, IUnit caster, IAbility ability, AbilityTarget target)
        {
            float score = 50; // Base score
            
            // Get target unit if applicable
            IUnit targetUnit = null;
            if (target.TargetUnitId.HasValue)
            {
                targetUnit = gameState.GetUnit(target.TargetUnitId.Value);
            }
            
            // Prioritize damage abilities against enemies
            if (ability is IDamageAbility damageAbility && targetUnit != null)
            {
                // Higher score for more damage
                score += damageAbility.Damage * 2;
                
                // Prioritize low HP enemies (potential kill)
                float hpPercent = (float)targetUnit.CurrentHP / targetUnit.MaxHP;
                if (hpPercent < 0.3f)
                {
                    score += 50; // Bonus for finishing off weak enemies
                }
                
                // Prioritize enemies in range
                int distance = CalculateDistance(caster.Position, targetUnit.Position);
                if (distance <= 1)
                {
                    score += 20; // Bonus for close targets
                }
            }
            
            // Prioritize healing low HP allies
            if (ability is IHealAbility healAbility && targetUnit != null)
            {
                float hpPercent = (float)targetUnit.CurrentHP / targetUnit.MaxHP;
                
                if (hpPercent < 0.5f)
                {
                    score += 100 - (hpPercent * 100); // Higher score for lower HP
                }
                else
                {
                    score -= 30; // Penalty for healing healthy allies
                }
            }
            
            // Bonus for status effect abilities
            if (ability is IStatusEffectAbility)
            {
                score += 30;
            }
            
            return score;
        }
        
        private List<ScoredAction> EvaluateMovementActions(ICombatState gameState, IUnit unit)
        {
            var scoredActions = new List<ScoredAction>();
            var validPositions = GetValidMovePositions(gameState, unit);
            
            foreach (var position in validPositions)
            {
                var score = EvaluatePosition(gameState, unit, position);
                scoredActions.Add(new ScoredAction(
                    new MoveAction(unit.Owner, unit.Id, position),
                    score
                ));
            }
            
            return scoredActions;
        }
        
        private float EvaluatePosition(ICombatState gameState, IUnit unit, HexCoordinates position)
        {
            float score = 30; // Base score
            
            // Find closest enemy
            var enemies = gameState.Units.Where(u => 
                u.Owner.Id != unit.Owner.Id && 
                u.IsAlive
            ).ToList();
            
            if (enemies.Count > 0)
            {
                var closestEnemy = enemies.OrderBy(e => CalculateDistance(position, e.Position)).First();
                int distanceToEnemy = CalculateDistance(position, closestEnemy.Position);
                
                // Prefer positions closer to enemies (but not too close if low HP)
                float hpPercent = (float)unit.CurrentHP / unit.MaxHP;
                
                if (hpPercent > 0.5f)
                {
                    // High HP: move closer
                    score += (10 - distanceToEnemy) * 5;
                }
                else
                {
                    // Low HP: keep distance
                    score += distanceToEnemy * 3;
                }
                
                // Avoid being surrounded
                int enemiesNearby = enemies.Count(e => CalculateDistance(position, e.Position) <= 2);
                score -= enemiesNearby * 15;
            }
            
            return score;
        }
        
        private List<AbilityTarget> GetValidTargetsForAbility(ICombatState gameState, IUnit caster, IAbility ability)
        {
            var targets = new List<AbilityTarget>();
            
            switch (ability.TargetType)
            {
                case AbilityTargetType.Self:
                    targets.Add(AbilityTarget.ForSelf());
                    break;
                    
                case AbilityTargetType.Enemy:
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
            int maxRange = 3;
            
            for (int q = -maxRange; q <= maxRange; q++)
            {
                for (int r = -maxRange; r <= maxRange; r++)
                {
                    if (q == 0 && r == 0)
                        continue;
                    
                    var pos = new HexCoordinates(unit.Position.Q + q, unit.Position.R + r);
                    
                    if (CalculateDistance(unit.Position, pos) > maxRange)
                        continue;
                    
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
        
        private struct ScoredAction
        {
            public IAction Action;
            public float Score;
            
            public ScoredAction(IAction action, float score)
            {
                Action = action;
                Score = score;
            }
        }
    }
}

