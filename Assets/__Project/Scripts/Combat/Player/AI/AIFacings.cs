using System.Collections.Generic;
using Combat.Config;
using Combat.Core;

namespace Combat.Player.AI
{
    /// <summary>
    /// The single source of the facings an AI considers per ability, in a fixed order so
    /// candidate enumeration (and therefore the seeded pick) is state-deterministic.
    /// </summary>
    public static class AIFacings
    {
        public static readonly HexDirection[] AllDirections =
        {
            HexDirection.E, HexDirection.NE, HexDirection.NW,
            HexDirection.W, HexDirection.SW, HexDirection.SE
        };

        public static IEnumerable<HexDirection?> For(IAbility ability)
        {
            if (ability.Shape.Type == AbilityShapeType.Ring)
            {
                yield return null; // Ring ignores facing.
            }
            else
            {
                foreach (var direction in AllDirections)
                    yield return direction;
            }
        }
    }
}
