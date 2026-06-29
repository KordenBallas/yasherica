namespace Narrative.Interaction.Core
{
    /// <summary>
    /// A ground-plane (XZ) position with no UnityEngine dependency, so the proximity geometry stays
    /// unit-testable. The MonoBehaviour layer converts a world <c>Vector3</c> into this (x, z).
    /// </summary>
    public readonly struct PlanarPoint
    {
        public readonly float X;
        public readonly float Z;

        public PlanarPoint(float x, float z)
        {
            X = x;
            Z = z;
        }

        /// <summary>Squared planar distance — compared against a squared radius to avoid a square root.</summary>
        public float SquaredDistanceTo(PlanarPoint other)
        {
            float dx = X - other.X;
            float dz = Z - other.Z;
            return dx * dx + dz * dz;
        }
    }
}
