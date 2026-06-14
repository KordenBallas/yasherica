using System;

namespace Character.Locomotion
{
    /// <summary>
    /// Pure locomotion logic (no UnityEngine, unit-tested): turns a planar velocity into a
    /// normalized animator speed and a smoothly-stepped facing yaw. Stateful only in the
    /// retained current yaw, so that when the character stops it keeps facing where it was
    /// (no snap) and turns are rate-limited rather than instantaneous.
    /// </summary>
    public sealed class LocomotionSolver
    {
        private const double DegreesPerRadian = 180d / Math.PI;

        private readonly double _moveThreshold;
        private readonly double _turnDegreesPerSecond;

        private double _currentYawDegrees;

        public LocomotionSolver(float moveThreshold, float turnDegreesPerSecond, float initialYawDegrees = 0f)
        {
            _moveThreshold = Math.Max(0d, moveThreshold);
            _turnDegreesPerSecond = Math.Max(0d, turnDegreesPerSecond);
            _currentYawDegrees = NormalizeSignedDegrees(initialYawDegrees);
        }

        /// <summary>
        /// Evaluates one frame. <paramref name="velocityX"/>/<paramref name="velocityZ"/> are the
        /// world-space planar velocity components; <paramref name="maxSpeed"/> normalizes the blend.
        /// Facing only updates while moving, so a stationary character holds its last heading.
        /// </summary>
        public LocomotionState Evaluate(float velocityX, float velocityZ, float maxSpeed, float deltaTime)
        {
            double planarSpeed = Math.Sqrt((double)velocityX * velocityX + (double)velocityZ * velocityZ);
            double normalized = maxSpeed > 0f ? Clamp01(planarSpeed / maxSpeed) : 0d;
            bool isMoving = planarSpeed > _moveThreshold;

            if (isMoving)
            {
                // Atan2(x, z) yields a yaw where 0 = +Z and +90 = +X, matching a Y-axis
                // rotation applied to the rig's forward (+Z) axis.
                double targetYaw = Math.Atan2(velocityX, velocityZ) * DegreesPerRadian;
                _currentYawDegrees = StepTowards(_currentYawDegrees, targetYaw, _turnDegreesPerSecond * Math.Max(0d, deltaTime));
            }

            return new LocomotionState((float)normalized, (float)_currentYawDegrees, isMoving);
        }

        /// <summary>Steps <paramref name="current"/> toward <paramref name="target"/> along the shortest arc, capped by <paramref name="maxDelta"/>.</summary>
        private static double StepTowards(double current, double target, double maxDelta)
        {
            double delta = NormalizeSignedDegrees(target - current);
            if (maxDelta <= 0d)
            {
                return current;
            }

            if (Math.Abs(delta) <= maxDelta)
            {
                return NormalizeSignedDegrees(target);
            }

            return NormalizeSignedDegrees(current + Math.Sign(delta) * maxDelta);
        }

        /// <summary>Wraps an angle into [-180, 180).</summary>
        private static double NormalizeSignedDegrees(double angle)
        {
            angle %= 360d;
            if (angle < -180d)
            {
                angle += 360d;
            }
            else if (angle >= 180d)
            {
                angle -= 360d;
            }

            return angle;
        }

        private static double Clamp01(double value)
        {
            if (value < 0d)
            {
                return 0d;
            }

            return value > 1d ? 1d : value;
        }
    }
}
