using System.Collections.Generic;
using System.Linq;
using Combat.Battlefield;
using Combat.Config;
using Combat.Core;
using UnityEngine;

namespace Combat.Player
{
    /// <summary>
    /// Maps a predicted AbilityOutcome into world space for the ghost view. Pure — the
    /// hex→world conversion is passed in, so the mapping is unit-testable.
    /// </summary>
    public static class GhostPlaybackPlanBuilder
    {
        public static GhostPlaybackPlan Build(
            AbilityOutcome outcome,
            IUnit caster,
            HexDirection? lineDirection,
            HexDirectionConfig hexConfig,
            System.Func<HexCoordinates, Vector3> hexToWorld)
        {
            var lookDirection = Vector3.zero;
            if (lineDirection.HasValue)
            {
                var neighbor = FacingGeometry.Neighbor(caster.Position, lineDirection.Value, hexConfig);
                lookDirection = hexToWorld(neighbor) - hexToWorld(caster.Position);
                lookDirection.y = 0f;
                if (lookDirection.sqrMagnitude > 0f)
                    lookDirection = lookDirection.normalized;
            }

            var markers = outcome.Units
                .Select(unit => new GhostUnitMarker(
                    unit.UnitId,
                    hexToWorld(unit.From),
                    hexToWorld(unit.To),
                    unit.Damage,
                    unit.Heal,
                    unit.IsDisplaced))
                .ToList();

            return new GhostPlaybackPlan(
                caster.Id,
                hexToWorld(caster.Position),
                lookDirection,
                markers);
        }
    }
}
