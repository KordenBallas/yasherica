using UnityEngine;

namespace Combat.Config
{
    /// <summary>
    /// Configuration for combat movement and animation settings.
    /// </summary>
    [CreateAssetMenu(fileName = "CombatMovementConfig", menuName = "Combat/Movement Config")]
    public class CombatMovementConfig : ScriptableObject
    {
        [Header("Movement Settings")]
        [Tooltip("Maximum number of cells a unit can move per turn")]
        public int maxMovementRange = 1;
        
        [Header("Animation Settings")]
        [Tooltip("Duration of movement animation between cells")]
        public float movementDuration = 0.4f;
        
        [Tooltip("Duration of entry animation when entering combat")]
        public float entryAnimationDuration = 0.8f;
        
        [Tooltip("Animation curve for movement interpolation")]
        public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        
        [Tooltip("Height offset for jump arc during movement")]
        public float heightArcOffset = 0.5f;
        
        [Header("Input Settings")]
        [Tooltip("Key to activate movement mode")]
        public KeyCode movementModeKey = KeyCode.M;
        
        [Tooltip("Input deadzone threshold")]
        public float inputDeadzone = 0.1f;
        
        [Tooltip("Layer mask for battlefield raycasting")]
        public LayerMask battlefieldRaycastMask;
        
        [Header("Visual Feedback")]
        [Tooltip("Color for hovered cells")]
        public Color hoveredCellColor = new Color(1f, 1f, 0f, 0.5f);

        [Tooltip("Color for valid move cells")]
        public Color validMoveCellColor = new Color(0f, 1f, 0f, 0.3f);

        [Tooltip("Color for invalid move cells")]
        public Color invalidMoveCellColor = new Color(1f, 0f, 0f, 0.3f);

        [Tooltip("Color for selected cells")]
        public Color selectedCellColor = new Color(0f, 0.5f, 1f, 0.6f);

        [Header("Ability Highlights")]
        [Tooltip("Color for ability range indicator")]
        public Color abilityRangeColor = new Color(0.5f, 0f, 1f, 0.2f);

        [Tooltip("Color for valid ability targets")]
        public Color validAbilityTargetColor = new Color(0f, 1f, 0.5f, 0.4f);

        [Tooltip("Color for invalid ability targets")]
        public Color invalidAbilityTargetColor = new Color(1f, 0f, 0f, 0.3f);
    }
}
