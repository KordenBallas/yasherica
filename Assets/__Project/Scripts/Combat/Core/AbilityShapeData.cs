namespace Combat.Core
{
    /// <summary>
    /// Immutable value type describing the area-of-effect shape of an ability.
    /// </summary>
    public readonly struct AbilityShapeData
    {
        public AbilityShapeType Type { get; }

        /// <summary>Width-1 cells straight from the caster in the chosen direction. (Line only)</summary>
        public int LineLength { get; }

        /// <summary>Cells at exactly this distance form the hollow ring. (Ring only)</summary>
        public int RingRadius { get; }

        private AbilityShapeData(AbilityShapeType type, int lineLength, int ringRadius)
        {
            Type = type;
            LineLength = lineLength;
            RingRadius = ringRadius;
        }

        public static AbilityShapeData ForLine(int length) =>
            new AbilityShapeData(AbilityShapeType.Line, length, 0);

        public static AbilityShapeData ForRing(int radius) =>
            new AbilityShapeData(AbilityShapeType.Ring, 0, radius);
    }
}
