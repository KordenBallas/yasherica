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

        [Header("Visuals")]
        [Tooltip("Material for the arena platform mesh; falls back to a plain lit material when empty.")]
        [SerializeField] private Material _platformMaterial;

        public int MaxPlayers => _maxPlayers;
        public int Port => _port;
        public bool OfflineMode => _offlineMode;
        public int OfflineDummyCount => _offlineDummyCount;
        public int OfflineMatchSeed => _offlineMatchSeed;
        public Material PlatformMaterial => _platformMaterial;
    }
}
