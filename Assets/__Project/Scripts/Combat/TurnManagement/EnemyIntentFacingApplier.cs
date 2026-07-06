using System.Collections.Generic;
using Combat.Config;
using Combat.Core;

namespace Combat.TurnManagement
{
    /// <summary>
    /// Turns every enemy toward its committed action the moment plans are revealed (D6):
    /// a committed move faces its step direction, a committed line ability faces its committed
    /// facing — so reading a unit's orientation is reading what it is about to do, uniformly
    /// for every armed enemy. Pure state-in/state-out; the model yaw follows via
    /// <c>UnitFacingRotator</c> on the resulting state change.
    /// </summary>
    public static class EnemyIntentFacingApplier
    {
        public static ICombatState Apply(
            ICombatState state,
            IReadOnlyList<EnemyIntent> intents,
            HexDirectionConfig hexConfig)
        {
            if (state == null || intents == null || hexConfig == null)
                return state;

            var newState = state;
            foreach (var intent in intents)
            {
                var unit = newState.GetUnit(intent.UnitId) as Unit;
                if (unit == null || !unit.IsAlive)
                    continue;

                HexDirection? facing = null;
                if (intent.IsMove && intent.MoveDestination.HasValue)
                    facing = FacingGeometry.DirectionFor(unit.Position, intent.MoveDestination.Value, hexConfig);
                else if (intent.CommittedFacing.HasValue)
                    facing = intent.CommittedFacing;

                if (facing.HasValue && unit.FacingDirection != facing.Value)
                    newState = (newState as CombatState).WithUpdatedUnit(unit.WithFacingDirection(facing.Value));
            }

            return newState;
        }
    }
}
