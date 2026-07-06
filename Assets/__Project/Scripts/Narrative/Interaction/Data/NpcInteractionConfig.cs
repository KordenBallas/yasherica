using Narrative.Interaction.Core;
using UnityEngine;

namespace Narrative.Interaction.Data
{
    /// <summary>
    /// Authored global config for NPC proximity (R13): the interaction radius (F-prompt range) and the
    /// aggro radius (auto-battle range), applied to all NPCs, plus the camp boss's larger engagement
    /// radius (the only per-role radius; a general per-NPC surface stays out of scope). Configuration
    /// data only — mapped to the pure-C# <see cref="NpcInteractionSettings"/> at install time.
    /// </summary>
    [CreateAssetMenu(fileName = "NpcInteractionConfig", menuName = "Narrative/NPC Interaction Config")]
    public class NpcInteractionConfig : ScriptableObject
    {
        [Tooltip("Range within which a talkable NPC shows the F prompt (world units)")]
        [SerializeField] private float _interactionRadius = 3.5f;

        [Tooltip("Range within which a hostile NPC starts the battle on its own (world units)")]
        [SerializeField] private float _aggroRadius = 2.5f;

        [Tooltip("A camp boss's engagement circle (world units) — noticeably larger than a normal NPC's; crossing it opens his talk or starts the camp fight")]
        [SerializeField] private float _bossEngagementRadius = 6f;

        public float InteractionRadius => _interactionRadius;
        public float AggroRadius => _aggroRadius;
        public float BossEngagementRadius => _bossEngagementRadius;
    }
}
