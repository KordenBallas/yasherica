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

        [Header("Threads (D13/D14)")]
        [Tooltip("Ceiling on simultaneously-live threads (ephemeral + arc). At the cap no new thread opens.")]
        [Min(1)]
        [SerializeField] private int _maxLiveThreads = 3;
        [Tooltip("Expiry lifespan (windows without advance) for thread labels with no ThreadDefinition asset.")]
        [Min(1)]
        [SerializeField] private int _defaultEphemeralLifespanWindows = 3;

        [Header("Spine reveal lane (D7)")]
        [Tooltip("Per-run cap on spine reveal-beats the reserved lane may place (lore-pacing: 1-2 so each registers). 0 disables the lane.")]
        [Min(0)]
        [SerializeField] private int _maxSpineRevealsPerRun = 2;

        public int WindowSize => _windowSize;
        public int LookAheadWindows => _lookAheadWindows;
        public int MaxLiveThreads => _maxLiveThreads;
        public int DefaultEphemeralLifespanWindows => _defaultEphemeralLifespanWindows;
        public int MaxSpineRevealsPerRun => _maxSpineRevealsPerRun;
    }
}
