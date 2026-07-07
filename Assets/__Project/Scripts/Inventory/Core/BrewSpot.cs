namespace Inventory.Core
{
    /// <summary>
    /// One stable bubble spot inside the brew volume, in bowl-local units (origin
    /// at the cauldron bottom centre, Y up). Spots are ordered bottom-up by the
    /// lattice builder; <see cref="Index"/> is that fill order. <see cref="Row"/>
    /// and <see cref="Column"/> address the spot in the lattice grid — a column
    /// is a vertical stack of spots sharing one X, the unit the gravity settle
    /// operates on (FR4: only the column above a removed bubble falls).
    /// </summary>
    public readonly struct BrewSpot
    {
        public int Index { get; }

        /// <summary>Lattice row, bottom-up (0 = the floor row).</summary>
        public int Row { get; }

        /// <summary>Lattice column key (0 = centre, positive right, negative left).</summary>
        public int Column { get; }

        public float X { get; }
        public float Y { get; }
        public float Z { get; }

        public BrewSpot(int index, int row, int column, float x, float y, float z)
        {
            Index = index;
            Row = row;
            Column = column;
            X = x;
            Y = y;
            Z = z;
        }
    }
}
