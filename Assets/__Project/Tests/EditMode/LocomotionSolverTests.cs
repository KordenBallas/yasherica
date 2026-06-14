using System;
using Character.Locomotion;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class LocomotionSolverTests
    {
        private const float MaxSpeed = 5f;
        private const float MoveThreshold = 0.1f;
        private const float FastTurn = 720f;

        private static (float vx, float vz) DirectionForYaw(float yawDegrees, float speed)
        {
            // Inverse of Atan2(x, z): x = sin(yaw), z = cos(yaw).
            var rad = yawDegrees * Math.PI / 180d;
            return ((float)(Math.Sin(rad) * speed), (float)(Math.Cos(rad) * speed));
        }

        [Test]
        public void Evaluate_NormalizesPlanarSpeedToMax()
        {
            var solver = new LocomotionSolver(MoveThreshold, FastTurn);

            var state = solver.Evaluate(0f, MaxSpeed * 0.5f, MaxSpeed, 0.1f);

            Assert.AreEqual(0.5f, state.NormalizedSpeed, 1e-4f);
            Assert.IsTrue(state.IsMoving);
        }

        [Test]
        public void Evaluate_ClampsNormalizedSpeedAtOne()
        {
            var solver = new LocomotionSolver(MoveThreshold, FastTurn);

            var state = solver.Evaluate(MaxSpeed * 2f, 0f, MaxSpeed, 0.1f);

            Assert.AreEqual(1f, state.NormalizedSpeed, 1e-4f);
        }

        [Test]
        public void Evaluate_BelowThreshold_IsNotMoving()
        {
            var solver = new LocomotionSolver(MoveThreshold, FastTurn);

            var state = solver.Evaluate(0.01f, 0f, MaxSpeed, 0.1f);

            Assert.IsFalse(state.IsMoving);
        }

        [TestCase(0f, 1f, 0f)]     // +Z -> 0
        [TestCase(1f, 0f, 90f)]    // +X -> +90
        [TestCase(-1f, 0f, -90f)]  // -X -> -90
        public void Evaluate_FacesCardinalDirection(float vx, float vz, float expectedYaw)
        {
            // A full second at a high turn rate guarantees the target is reached this step.
            var solver = new LocomotionSolver(MoveThreshold, FastTurn);

            var state = solver.Evaluate(vx * MaxSpeed, vz * MaxSpeed, MaxSpeed, 1f);

            Assert.AreEqual(expectedYaw, state.YawDegrees, 1e-3f);
        }

        [Test]
        public void Evaluate_Backward_FacesOneHundredEighty()
        {
            var solver = new LocomotionSolver(MoveThreshold, FastTurn);

            var state = solver.Evaluate(0f, -MaxSpeed, MaxSpeed, 1f);

            Assert.AreEqual(180f, Math.Abs(state.YawDegrees), 1e-3f);
        }

        [Test]
        public void Evaluate_TurnIsRateLimited_ShortestArc()
        {
            // Facing -10deg, moving toward +10deg, with a 10deg budget this step -> lands on 0.
            var solver = new LocomotionSolver(MoveThreshold, 100f, initialYawDegrees: -10f);
            var (vx, vz) = DirectionForYaw(10f, MaxSpeed);

            var state = solver.Evaluate(vx, vz, MaxSpeed, 0.1f);

            Assert.AreEqual(0f, state.YawDegrees, 1e-3f);
        }

        [Test]
        public void Evaluate_TurnWrapsAcrossOneHundredEighty()
        {
            // Facing 170deg, moving toward -170deg: the short arc crosses +/-180, not through 0.
            var solver = new LocomotionSolver(MoveThreshold, 100f, initialYawDegrees: 170f);
            var (vx, vz) = DirectionForYaw(-170f, MaxSpeed);

            var state = solver.Evaluate(vx, vz, MaxSpeed, 0.1f);

            // One 10deg step the short way lands on the +/-180 boundary (not 160).
            Assert.AreEqual(180f, Math.Abs(state.YawDegrees), 1e-3f);
        }

        [Test]
        public void Evaluate_WhenStopped_KeepsPreviousYaw()
        {
            var solver = new LocomotionSolver(MoveThreshold, FastTurn);
            solver.Evaluate(MaxSpeed, 0f, MaxSpeed, 1f); // face +X (90deg)

            var state = solver.Evaluate(0f, 0f, MaxSpeed, 1f); // now idle

            Assert.IsFalse(state.IsMoving);
            Assert.AreEqual(90f, state.YawDegrees, 1e-3f);
        }
    }
}
