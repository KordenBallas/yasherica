using UnityEngine;

namespace Inventory.Data.Definitions
{
    /// <summary>
    /// ScriptableObject describing one function trait (e.g. 'sharp', 'stone').
    /// Contains ONLY configuration data - NO logic.
    /// Traits are an authorable vocabulary: a designer adds one by creating an
    /// asset, never code. Artifacts and fusion rules reference traits by asset;
    /// pure-C# scoring sees only the <see cref="Id"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "TraitDefinition", menuName = "Inventory/Trait")]
    public class TraitDefinition : ScriptableObject
    {
        [Header("Identity")]
        [Tooltip("Stable unique id referenced across artifacts, fusion rules, and part affinities (e.g. 'sharp')")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName;
        [TextArea(2, 4)]
        [SerializeField] private string _description;

        [Header("Vocabulary")]
        [Tooltip("Substance = what it is made of; Property = what it does")]
        [SerializeField] private TraitAxis _axis;

        public string Id => _id;
        public string DisplayName => _displayName;
        public string Description => _description;
        public TraitAxis Axis => _axis;
    }
}
