using Narrative.Interaction.Core;
using UnityEngine;

namespace Narrative.Interaction.Data
{
    /// <summary>
    /// Authored global config for NPC proximity (R13): the interaction radius (F-prompt range) and the
    /// aggro radius (auto-battle range), applied to all NPCs. Configuration data only — mapped to the
    /// pure-C# <see cref="NpcInteractionSettings"/> at install time. Per-NPC overrides are out of scope.
    /// </summary>
    [CreateAssetMenu(fileName = "NpcInteractionConfig", menuName = "Narrative/NPC Interaction Config")]
    public class NpcInteractionConfig : ScriptableObject
    {
        [Tooltip("Range within which a talkable NPC shows the F prompt (world units)")]
        [SerializeField] private float _interactionRadius = 3.5f;

        [Tooltip("Range within which a hostile NPC starts the battle on its own (world units)")]
        [SerializeField] private float _aggroRadius = 2.5f;

        public float InteractionRadius => _interactionRadius;
        public float AggroRadius => _aggroRadius;
    }
}
