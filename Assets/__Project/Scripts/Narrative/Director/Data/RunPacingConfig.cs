using UnityEngine;

namespace Narrative.Director.Data
{
    /// <summary>
    /// ScriptableObject window mechanics for the windowed director. Configuration data only — the
    /// planner consumes the mapped Core <c>RunPacingSettings</c>, never this SO. A "window" is the next
    /// <see cref="WindowSize"/> platforms ahead of the player. World fullness (quest rarity, ambient
    /// monster/loot/empty mix) lives on <c>WorldContentDensityConfig</c>, not here.
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

        public int WindowSize => _windowSize;
        public int LookAheadWindows => _lookAheadWindows;
    }
}
