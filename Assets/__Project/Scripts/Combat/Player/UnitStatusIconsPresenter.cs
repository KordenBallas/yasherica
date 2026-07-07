using System;
using System.Collections.Generic;
using System.Linq;
using Combat.Controller;
using Combat.Core;

namespace Combat.Player
{
    /// <summary>
    /// Presents every unit's active statuses as an on-unit glyph row (combat-status-effects
    /// S2/FR7): icon + remaining turns per status, cleared the moment a status expires or a
    /// unit dies. Rebuilds from the immutable state on every state change, so the row can
    /// never drift from the sim — PvE and Arena share this presenter through the
    /// ICombatController seam.
    /// </summary>
    public class UnitStatusIconsPresenter : IDisposable
    {
        private readonly ICombatController _combatController;
        private readonly IUnitStatusIconsView _view;

        public UnitStatusIconsPresenter(ICombatController combatController, IUnitStatusIconsView view)
        {
            _combatController = combatController;
            _view = view;

            _combatController.OnStateChanged += HandleStateChanged;
        }

        public void Dispose()
        {
            _combatController.OnStateChanged -= HandleStateChanged;
            _view.ClearAll();
        }

        private void HandleStateChanged(ICombatState state)
        {
            if (state == null) return;

            foreach (var unit in state.Units)
            {
                _view.SetIcons(unit.Id, BuildIconsFor(unit));
            }
        }

        private static IReadOnlyList<StatusIconModel> BuildIconsFor(IUnit unit)
        {
            if (!unit.IsAlive)
                return Array.Empty<StatusIconModel>();

            if (unit.StatusEffects.Count == 0)
                return Array.Empty<StatusIconModel>();

            return unit.StatusEffects
                .Select(effect => new StatusIconModel(unit.Id, effect.Id, effect.Duration, effect.StackCount))
                .ToList();
        }
    }
}
