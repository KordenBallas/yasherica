namespace Inventory.Core
{
    /// <summary>
    /// One bubble's position and size in pot-local space (origin at the pot
    /// interior center, X right, Y up, Z into the liquid depth).
    /// </summary>
    public readonly struct BubblePlacement
    {
        public float X { get; }
        public float Y { get; }
        public float Z { get; }
        public float Radius { get; }

        /// <summary>Phase offset so neighbouring bubbles bob out of sync.</summary>
        public float BobPhase { get; }

        public BubblePlacement(float x, float y, float z, float radius, float bobPhase)
        {
            X = x;
            Y = y;
            Z = z;
            Radius = radius;
            BobPhase = bobPhase;
        }
    }
}
