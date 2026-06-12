namespace Inventory.Core
{
    /// <summary>
    /// One point of a lathe profile in axial 2D space: distance from the rotation
    /// axis and height above the profile's base.
    /// </summary>
    public readonly struct ProfilePoint
    {
        public float Radius { get; }
        public float Height { get; }

        public ProfilePoint(float radius, float height)
        {
            Radius = radius;
            Height = height;
        }
    }
}
