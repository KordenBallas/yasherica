using System;
using System.Collections.Generic;
using Combat.Battlefield;
using Combat.Config;

namespace Combat.Core
{
    public interface IAbilityShapeCalculator
    {
        /// <summary>
        /// Returns all hex cells affected by an ability given the caster's current position.
        /// For Line abilities, direction is required. For Ring, direction is ignored.
        /// </summary>
        IReadOnlyList<HexCoordinates> GetAffectedCells(
            AbilityShapeData shape,
            HexCoordinates casterPosition,
            HexDirection? direction,
            Func<HexCoordinates, bool> isCellInBoundary);
    }
}
