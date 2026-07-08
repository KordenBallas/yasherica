using Combat.Arena.Core;
using UnityEngine;

namespace Combat.Arena.Data
{
    /// <summary>
    /// Arena match dials — configuration only, no logic. Auto-loaded by <c>ArenaInstaller</c>
    /// from <c>Resources/Arena/ArenaMatchConfig</c> when not wired in the scene.
    /// </summary>
    [CreateAssetMenu(fileName = "ArenaMatchConfig", menuName = "Yasherica/Arena/Match Config")]
    public class ArenaMatchConfig : ScriptableObject
    {
        [Header("Session")]
        [Tooltip("Hard cap on players in a match (the brief says 2–4).")]
        [Range(2, 4)]
        [SerializeField] private int _maxPlayers = 4;

        [Tooltip("Direct-connect port for the networked session.")]
        [SerializeField] private int _port = 7777;

        [Header("Offline mode (dev fallback — seeded AI dummies instead of the network)")]
        [Tooltip("Skip the host/join flow and start an offline match vs AI dummies immediately.")]
        [SerializeField] private bool _offlineMode;

        [Tooltip("How many AI dummies join the local player in offline mode.")]
        [Range(1, 3)]
        [SerializeField] private int _offlineDummyCount = 2;

        [Tooltip("Match seed for offline mode; 0 rolls a fresh seed each launch (logged).")]
        [SerializeField] private int _offlineMatchSeed;

        [Header("Reconnect & migration (X1)")]
        [Tooltip("Rounds a disconnected seat is auto-passed (unit stays alive) before it departs for good.")]
        [Range(0, 10)]
        [SerializeField] private int _disconnectGraceRounds = 3;

        [Tooltip("How long a dropped client keeps retrying the known host before electing a new one.")]
        [SerializeField] private float _reconnectAttemptSeconds = 10f;

        [Tooltip("Delay between reconnect attempts to the same address.")]
        [SerializeField] private float _reconnectRetryIntervalSeconds = 2f;

        [Tooltip("How long a client tries to reach the elected migration host before giving up.")]
        [SerializeField] private float _migrationConnectTimeoutSeconds = 12f;

        [Header("Rules & anti-cheat (X2)")]
        [Tooltip("Resolution-order strategy: rotating initiative (default) or a per-round seeded shuffle (P4-3a).")]
        [SerializeField] private ArenaResolutionOrderMode _resolutionOrderMode =
            ArenaResolutionOrderMode.RotatingInitiative;

        [Tooltip("Host re-validates every relayed commit against its canonical state; an invalid commit is replaced with a pass.")]
        [SerializeField] private bool _validateCommits = true;

        [Tooltip("Per-step simultaneous damage batching (P4-3b): a mutual lethal exchange kills both (possible draw). OFF keeps the shipped sequential skip-dead rule.")]
        [SerializeField] private bool _simultaneousDamageBatching;

        [Header("Visuals")]
        [Tooltip("Material for the arena platform mesh; falls back to a plain lit material when empty.")]
        [SerializeField] private Material _platformMaterial;

        public int MaxPlayers => _maxPlayers;
        public int Port => _port;
        public bool OfflineMode => _offlineMode;
        public int OfflineDummyCount => _offlineDummyCount;
        public int OfflineMatchSeed => _offlineMatchSeed;
        public int DisconnectGraceRounds => _disconnectGraceRounds;
        public float ReconnectAttemptSeconds => _reconnectAttemptSeconds;
        public float ReconnectRetryIntervalSeconds => _reconnectRetryIntervalSeconds;
        public float MigrationConnectTimeoutSeconds => _migrationConnectTimeoutSeconds;
        public ArenaResolutionOrderMode ResolutionOrderMode => _resolutionOrderMode;
        public bool ValidateCommits => _validateCommits;
        public bool SimultaneousDamageBatching => _simultaneousDamageBatching;
        public Material PlatformMaterial => _platformMaterial;
    }
}
