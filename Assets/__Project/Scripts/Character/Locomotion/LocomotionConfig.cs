using UnityEngine;

namespace Character.Locomotion
{
    /// <summary>
    /// Data-only tuning for movement-driven locomotion: the speed below which the character
    /// is treated as idle and how fast it turns to face its movement direction. Consumed by
    /// the <see cref="LocomotionSolver"/> via CharacterLocomotionInstaller.
    /// </summary>
    [CreateAssetMenu(fileName = "LocomotionConfig", menuName = "Character System/Locomotion Config")]
    public class LocomotionConfig : ScriptableObject
    {
        [Tooltip("Planar speed (units/sec) below which the character is idle: no run blend, no facing update.")]
        [SerializeField] private float _moveThresholdSpeed = 0.1f;

        [Tooltip("How fast the character yaws toward its movement direction (degrees/sec).")]
        [SerializeField] private float _turnDegreesPerSecond = 720f;

        public float MoveThresholdSpeed => _moveThresholdSpeed;
        public float TurnDegreesPerSecond => _turnDegreesPerSecond;
    }
}
