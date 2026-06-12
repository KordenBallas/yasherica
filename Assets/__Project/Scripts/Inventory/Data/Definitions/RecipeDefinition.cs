using System.Collections.Generic;
using UnityEngine;

namespace Inventory.Data.Definitions
{
    /// <summary>
    /// ScriptableObject describing one crafting recipe: an unordered set of input
    /// artifacts producing one output artifact.
    /// Contains ONLY configuration data - NO logic; matching lives in RecipeBook.
    /// </summary>
    [CreateAssetMenu(fileName = "RecipeDefinition", menuName = "Inventory/Recipe")]
    public class RecipeDefinition : ScriptableObject
    {
        [Tooltip("Input artifacts; order does not matter, duplicates are allowed")]
        [SerializeField] private List<ArtifactDefinition> _inputs;

        [Tooltip("Artifact produced when the inputs are combined")]
        [SerializeField] private ArtifactDefinition _output;

        public IReadOnlyList<ArtifactDefinition> Inputs => _inputs;
        public ArtifactDefinition Output => _output;
    }
}
