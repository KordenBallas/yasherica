using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;

namespace Combat.Execution
{
    /// <summary>
    /// Mirrors AbilityExecutor's semantics (damage first, then farthest-first push with the
    /// same DisplacementResolver) over a working copy of positions and HP, so the ghost's
    /// predicted numbers and destinations match execution exactly on an unchanged board.
    /// </summary>
    public class AbilityOutcomeCalculator : IAbilityOutcomeCalculator
    {
        private readonly IAbilityShapeCalculator _shapeCalculator;
        private readonly IDamageSystem _damageSystem;
        private readonly HexDirectionConfig _hexConfig;

        public AbilityOutcomeCalculator(
            IAbilityShapeCalculator shapeCalculator,
            IDamageSystem damageSystem,
            HexDirectionConfig hexConfig)
        {
            _shapeCalculator = shapeCalculator;
            _damageSystem = damageSystem;
            _hexConfig = hexConfig;
        }

        public AbilityOutcome ComputeForFacing(ICombatState state, IUnit caster, IAbility ability, HexDirection facing)
        {
            var direction = ability.Shape.Type == AbilityShapeType.Line
                ? facing
                : (HexDirection?)null;

            var cells = _shapeCalculator.GetAffectedCells(
                ability.Shape, caster.Position, direction, state.IsPositionValid);

            return Compute(state, caster, ability, cells, direction);
        }

        public AbilityOutcome ComputeCommitted(ICombatState state, EnemyIntent intent)
        {
            if (!intent.IsAbility)
                return new AbilityOutcome(null, null);

            var caster = state.GetUnit(intent.UnitId);
            var abilityInstance = caster?.GetAbility(intent.AbilityId);
            if (abilityInstance == null)
                return new AbilityOutcome(null, null);

            return Compute(state, caster, abilityInstance.Ability, intent.CommittedCells, intent.CommittedFacing);
        }

        private AbilityOutcome Compute(
            ICombatState state,
            IUnit caster,
            IAbility ability,
            IReadOnlyList<HexCoordinates> cells,
            HexDirection? lineDirection)
        {
            // Working copies — prediction never touches the state.
            var predictedHp = state.Units.ToDictionary(u => u.Id, u => u.CurrentHP);
            var predictedPos = state.Units.ToDictionary(u => u.Id, u => u.Position);

            var damageDealt = new Dictionary<int, int>();
            var healDealt = new Dictionary<int, int>();
            var struckIds = new List<int>();

            foreach (var cell in cells)
            {
                var unitAtCell = state.GetUnitAt(cell);
                if (unitAtCell == null || !unitAtCell.IsAlive)
                    continue;

                int damage = ability is IDamageAbility damageAbility
                    ? _damageSystem.CalculateFinalDamage(caster, unitAtCell, damageAbility.Damage)
                    : 0;
                int heal = ability is IHealAbility healAbility ? healAbility.HealAmount : 0;

                predictedHp[unitAtCell.Id] = System.Math.Max(
                    0, System.Math.Min(predictedHp[unitAtCell.Id] - damage + heal, unitAtCell.MaxHP));
                damageDealt[unitAtCell.Id] = damage;
                healDealt[unitAtCell.Id] = heal;
                struckIds.Add(unitAtCell.Id);
            }

            // Displacement mirrors the executor: survivors only, farthest-first, occupancy
            // checked against the working positions so chained pushes never overlap.
            if (ability is IDisplacementAbility displacement
                && displacement.PushDistance > 0
                && lineDirection.HasValue)
            {
                for (int i = cells.Count - 1; i >= 0; i--)
                {
                    var unitAtCell = state.GetUnitAt(cells[i]);
                    if (unitAtCell == null || unitAtCell.Id == caster.Id)
                        continue;
                    if (predictedHp[unitAtCell.Id] <= 0)
                        continue;

                    int pushedId = unitAtCell.Id;
                    predictedPos[pushedId] = DisplacementResolver.ResolveDestination(
                        predictedPos[pushedId],
                        lineDirection.Value,
                        displacement.PushDistance,
                        _hexConfig,
                        state.IsPositionValid,
                        cell => predictedPos.Any(kv => kv.Key != pushedId && kv.Value.Equals(cell)));
                }
            }

            var unitOutcomes = struckIds
                .Select(id => new UnitOutcome(
                    id,
                    damageDealt[id],
                    healDealt[id],
                    state.GetUnit(id).Position,
                    predictedPos[id]))
                .ToList();

            return new AbilityOutcome(cells, unitOutcomes);
        }
    }
}
