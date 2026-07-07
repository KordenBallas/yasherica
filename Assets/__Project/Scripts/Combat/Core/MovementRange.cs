using Combat.Core.StatusEffects;

namespace Combat.Core
{
    /// <summary>
    /// The single source of a unit's effective per-round movement budget: the base range
    /// gated by Control statuses (Root → 0, Slow → base − penalty·stacks). Consumed by the
    /// action validator, the movement rules, and every enemy AI so the player input path and
    /// the planning path can never disagree.
    /// </summary>
    public static class MovementRange
    {
        /// <summary>Default movement range in hex cells when no per-unit base is supplied.</summary>
        public const int DefaultBase = 3;

        public static int EffectiveFor(IUnit unit, int baseRange = DefaultBase)
        {
            var range = baseRange;

            foreach (var effect in unit.StatusEffects)
            {
                if (!(effect is DataDrivenControlEffect control))
                    continue;

                if (control.Kind == ControlKind.Root)
                    return 0;

                if (control.Kind == ControlKind.Slow)
                    range -= control.MovementPenalty * control.StackCount;
            }

            return System.Math.Max(0, range);
        }
    }
}
