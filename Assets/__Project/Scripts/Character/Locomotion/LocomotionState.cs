namespace Character.Locomotion
{
    /// <summary>
    /// Result of one locomotion evaluation: how fast the character is moving (normalized
    /// 0..1 for the animator blend), the yaw it should face this frame, and whether it is
    /// moving at all. A pure value type — no UnityEngine dependency.
    /// </summary>
    public readonly struct LocomotionState
    {
        public LocomotionState(float normalizedSpeed, float yawDegrees, bool isMoving)
        {
            NormalizedSpeed = normalizedSpeed;
            YawDegrees = yawDegrees;
            IsMoving = isMoving;
        }

        /// <summary>Planar speed mapped to 0..1 (idle..max), used as the animator blend parameter.</summary>
        public float NormalizedSpeed { get; }

        /// <summary>Yaw (degrees, 0 = +Z) the character should face this frame.</summary>
        public float YawDegrees { get; }

        /// <summary>True when planar speed exceeds the configured movement threshold.</summary>
        public bool IsMoving { get; }
    }
}
