using UnityEngine;
using Zenject;

namespace Character.Locomotion
{
    /// <summary>
    /// Locomotion presenter (pure C#, not a MonoBehaviour): each tick it samples the velocity
    /// provider, runs the pure <see cref="LocomotionSolver"/>, and pushes the results to the
    /// view. Facing is applied only while moving so a stationary character keeps whatever
    /// heading other systems (e.g. forced turn-to-camera) have set.
    /// </summary>
    public class CharacterLocomotionPresenter : ITickable
    {
        private readonly ICharacterVelocityProvider _velocity;
        private readonly ILocomotionView _view;
        private readonly LocomotionSolver _solver;

        public CharacterLocomotionPresenter(
            ICharacterVelocityProvider velocity,
            ILocomotionView view,
            LocomotionSolver solver)
        {
            _velocity = velocity;
            _view = view;
            _solver = solver;
        }

        public void Tick()
        {
            var planar = _velocity.PlanarVelocity;
            var state = _solver.Evaluate(planar.x, planar.z, _velocity.MaxPlanarSpeed, Time.deltaTime);

            _view.SetMotionSpeed(state.NormalizedSpeed);
            if (state.IsMoving)
            {
                _view.SetFacingYaw(state.YawDegrees);
            }
        }
    }
}
