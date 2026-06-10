using Combat.Config;

namespace Combat.Core
{
    /// <summary>
    /// Represents the directional target of an ability.
    /// Line abilities require a direction; Ring abilities use None.
    /// Cells are recomputed from the caster's current position at execution time.
    /// </summary>
    public readonly struct AbilityTarget
    {
        public HexDirection? Direction { get; }
        public bool HasDirection => Direction.HasValue;

        private AbilityTarget(HexDirection? direction)
        {
            Direction = direction;
        }

        public static AbilityTarget ForDirection(HexDirection direction) =>
            new AbilityTarget(direction);

        public static AbilityTarget None() =>
            new AbilityTarget(null);
    }
}
