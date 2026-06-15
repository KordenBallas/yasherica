using System.Collections.Generic;
using Mutation.Data.Definitions;
using UnityEngine;

namespace Inventory.Data.Definitions
{
    /// <summary>
    /// ScriptableObject describing one artifact type.
    /// Contains ONLY configuration data - NO logic.
    /// Runtime artifacts are ArtifactInstance objects referencing this definition by Id.
    /// </summary>
    [CreateAssetMenu(fileName = "ArtifactDefinition", menuName = "Inventory/Artifact")]
    public class ArtifactDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id referenced by recipes and save data (e.g. 'fire')")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Visual")]
        [SerializeField] private Sprite _icon;
        [Tooltip("Tint of the bubble carrying this artifact inside the pot")]
        [SerializeField] private Color _bubbleTint = Color.white;

        [Header("Mutation")]
        [Tooltip("How eating this artifact pushes the character toward each creature archetype. " +
                 "Each entry references an ArchetypeDefinition by id; duplicate ids are summed.")]
        [SerializeField] private ArchetypeWeight[] _archetypeWeights;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Sprite Icon => _icon;
        public Color BubbleTint => _bubbleTint;
        public IReadOnlyList<ArchetypeWeight> ArchetypeWeights => _archetypeWeights;
    }
}
