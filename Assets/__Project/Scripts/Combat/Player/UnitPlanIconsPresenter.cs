using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Controller;
using Combat.Core;

namespace Combat.Player
{
    /// <summary>
    /// Presents each unit's plan as icons above it: the player's queued abilities in order,
    /// and every enemy's committed intent from the moment it is revealed at Plan phase —
    /// the readable "plan on the board" half of the ghost telegraph.
    /// </summary>
    public class UnitPlanIconsPresenter : IDisposable
    {
        private readonly ICombatController _combatController;
        private readonly IUnitPlanIconsView _view;

        public UnitPlanIconsPresenter(ICombatController combatController, IUnitPlanIconsView view)
        {
            _combatController = combatController;
            _view = view;

            _combatController.OnStateChanged += HandleStateChanged;
            _combatController.OnEnemyPlansRevealed += HandlePlansRevealed;
        }

        public void Dispose()
        {
            _combatController.OnStateChanged -= HandleStateChanged;
            _combatController.OnEnemyPlansRevealed -= HandlePlansRevealed;
            _view.ClearAll();
        }

        private void HandlePlansRevealed(IReadOnlyList<EnemyIntent> intents)
        {
            Rebuild(_combatController.CombatState);
        }

        private void HandleStateChanged(ICombatState state)
        {
            Rebuild(state);
        }

        private void Rebuild(ICombatState state)
        {
            if (state == null) return;

            foreach (var unit in state.Units)
            {
                _view.SetIcons(unit.Id, BuildIconsFor(state, unit));
            }
        }

        private static IReadOnlyList<PlanIconModel> BuildIconsFor(ICombatState state, IUnit unit)
        {
            if (!unit.IsAlive)
                return Array.Empty<PlanIconModel>();

            if (unit.Owner.Type == PlayerType.Human)
            {
                return unit.AbilityQueue
                    .OrderBy(scheduled => scheduled.ExecutionOrder)
                    .Select(scheduled => PlanIconModel.QueuedAbility(
                        unit.Id, scheduled.Ability.Ability.Id, scheduled.ExecutionOrder))
                    .ToList();
            }

            var intent = state.EnemyIntents.FirstOrDefault(i => i.UnitId == unit.Id);
            if (intent == null)
                return Array.Empty<PlanIconModel>();

            if (intent.IsAbility)
                return new[] { PlanIconModel.EnemyAbilityIntent(unit.Id, intent.AbilityId) };
            if (intent.IsMove)
                return new[] { PlanIconModel.EnemyMoveIntent(unit.Id) };

            return Array.Empty<PlanIconModel>();
        }
    }
}
