using UnityEngine;

namespace Mutation.Data.Definitions
{
    /// <summary>
    /// ScriptableObject describing one creature archetype axis (e.g. Reptile, Insect).
    /// Contains ONLY configuration data - NO logic.
    /// Archetypes are an authorable set: a designer adds one by creating an asset, never code.
    /// Artifacts reference archetypes by <see cref="Id"/> through their archetype weights.
    /// </summary>
    [CreateAssetMenu(fileName = "ArchetypeDefinition", menuName = "Mutation/Archetype")]
    public class ArchetypeDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id referenced by artifact archetype weights (e.g. 'reptile')")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Visual")]
        [Tooltip("Accent colour used by later mutation UI to represent this archetype")]
        [SerializeField] private Color _tint = Color.white;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public Color Tint => _tint;
    }
}
