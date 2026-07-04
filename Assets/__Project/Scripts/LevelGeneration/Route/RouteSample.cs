namespace LevelGeneration.Route
{
    /// <summary>
    /// The route's answer for one forward position: where the trail sits laterally (world Z, depth
    /// toward/away from the camera) and which elevation tier it stands on (world Y). Layout/read
    /// only — never a traversal rule.
    /// </summary>
    public readonly struct RouteSample
    {
        public readonly float LateralZ;
        public readonly float TierY;
        public readonly int TierIndex;

        public RouteSample(float lateralZ, float tierY, int tierIndex)
        {
            LateralZ = lateralZ;
            TierY = tierY;
            TierIndex = tierIndex;
        }
    }
}
