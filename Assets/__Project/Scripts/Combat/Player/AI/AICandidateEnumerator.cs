using System.Collections.Generic;
using System.Linq;
using Combat.Core;

namespace Combat.Player.AI
{
    /// <summary>
    /// Enumerates every option a unit can commit this round, in a fixed order (ability list
    /// order × fixed facing order, then move cells sorted by (Q, R), then end turn) so the
    /// seeded decision stream never depends on battlefield iteration order.
    /// </summary>
    public sealed class AICandidateEnumerator
    {
        public IReadOnlyList<AICandidate> Enumerate(ICombatState state, IUnit unit, AITuning tuning)
        {
            var candidates = new List<AICandidate>();

            foreach (var abilityInstance in unit.GetAvailableAbilities())
            {
                var ability = abilityInstance.Ability;
                foreach (var facing in AIFacings.For(ability))
                {
                    candidates.Add(AICandidate.ForAbility(
                        new ScheduleAbilityAction(unit.Owner, unit.Id, ability.Id, facing),
                        ability, facing));
                }
            }

            var range = MovementRange.EffectiveFor(unit, tuning.MovementRange);
            var destinations = state.GetValidPositionsInRange(unit.Position, range)
                .OrderBy(cell => cell.Q)
                .ThenBy(cell => cell.R)
                .Take(AITuning.MaxEvaluatedMovePositions);

            foreach (var destination in destinations)
            {
                candidates.Add(AICandidate.ForMove(
                    new MoveAction(unit.Owner, unit.Id, destination), destination));
            }

            candidates.Add(AICandidate.ForEndTurn(new EndUnitTurnAction(unit.Owner, unit.Id)));
            return candidates;
        }
    }
}
