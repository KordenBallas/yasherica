using UnityEngine;

namespace Hub.Data
{
    /// <summary>
    /// The Hub scene's world dials (O1 rework): the deterministic platform shape seed, the
    /// junk-keeper NPC's display name, and the two interaction radii. Configuration data only —
    /// NO logic (CLAUDE.md §7). The platform's LOOK is not here: it is authored as the **Hub
    /// biome** (`BiomeAppearance_Hub` → its feature kit's ground material), like any biome.
    /// </summary>
    [CreateAssetMenu(fileName = "HubSceneConfig", menuName = "Hub/Scene Config")]
    public class HubSceneConfig : ScriptableObject
    {
        [Header("Platform")]
        [Tooltip("Deterministic seed for the platform's hex shape (the Hub always looks the same).")]
        [SerializeField] private int _platformSeed = 777;

        [Header("Junk keeper (the part-offer NPC)")]
        [Tooltip("Display name over the NPC's head.")]
        [SerializeField] private string _npcDisplayName = "Junk Keeper";

        [Tooltip("Radius of the NPC's F-interaction circle (world units).")]
        [SerializeField] private float _npcInteractRadius = 2.5f;

        [Header("Portals")]
        [Tooltip("Radius of each portal's F-interaction circle (world units) — deliberately small.")]
        [SerializeField] private float _portalInteractRadius = 2f;

        public int PlatformSeed => _platformSeed;
        public string NpcDisplayName => _npcDisplayName;
        public float NpcInteractRadius => _npcInteractRadius;
        public float PortalInteractRadius => _portalInteractRadius;
    }
}
