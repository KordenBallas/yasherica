using UnityEngine;

namespace Narrative.Director.Data
{
    /// <summary>
    /// ScriptableObject pacing budget for the windowed director (P2). Configuration data only — the
    /// planner consumes the mapped Core <c>RunPacingSettings</c>, never this SO. A "window" is the next
    /// <see cref="WindowSize"/> platforms ahead of the player.
    /// </summary>
    [CreateAssetMenu(fileName = "RunPacingConfig", menuName = "Narrative/Director/Run Pacing Config")]
    public class RunPacingConfig : ScriptableObject
    {
        [Header("Window")]
        [Tooltip("Platforms per planning window (the look-ahead the director commits at once)")]
        [Min(1)]
        [SerializeField] private int _windowSize = 4;
        [Tooltip("How many windows are kept planned ahead (1 committed + lookahead)")]
        [Min(1)]
        [SerializeField] private int _lookAheadWindows = 1;

        [Header("Narrative Budget")]
        [Tooltip("Maximum sum of selected story weights per window")]
        [Min(0)]
        [SerializeField] private int _narrativeBudgetPerWindow = 30;

        [Header("Combat Budget (separate dimension)")]
        [Tooltip("Minimum combat-bearing encounters per window")]
        [Min(0)]
        [SerializeField] private int _minCombatPerWindow = 1;
        [Tooltip("Maximum combat-bearing encounters per window")]
        [Min(0)]
        [SerializeField] private int _maxCombatPerWindow = 2;

        public int WindowSize => _windowSize;
        public int LookAheadWindows => _lookAheadWindows;
        public int NarrativeBudgetPerWindow => _narrativeBudgetPerWindow;
        public int MinCombatPerWindow => _minCombatPerWindow;
        public int MaxCombatPerWindow => _maxCombatPerWindow;
    }
}
