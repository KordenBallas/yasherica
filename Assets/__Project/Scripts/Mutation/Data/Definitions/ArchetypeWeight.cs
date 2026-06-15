using System;
using UnityEngine;

namespace Mutation.Data.Definitions
{
    /// <summary>
    /// One authored contribution of an artifact toward a single creature archetype axis.
    /// Configuration data only; the archetype is referenced by id (see <see cref="ArchetypeDefinition.Id"/>).
    /// Serialized as an array on <c>ArtifactDefinition</c> because Unity cannot serialize a Dictionary;
    /// duplicate ids are summed by the mapper, so authoring order is irrelevant.
    /// </summary>
    [Serializable]
    public class ArchetypeWeight
    {
        [Tooltip("ArchetypeDefinition.Id this weight contributes to (e.g. 'reptile')")]
        [SerializeField] private string _archetypeId;

        [Tooltip("How strongly eating this artifact pushes the character toward the archetype")]
        [SerializeField] private float _weight = 1f;

        public string ArchetypeId => _archetypeId;
        public float Weight => _weight;

        // Parameterless ctor kept for Unity serialization.
        public ArchetypeWeight()
        {
        }

        // Convenience ctor for programmatic creation and tests.
        public ArchetypeWeight(string archetypeId, float weight)
        {
            _archetypeId = archetypeId;
            _weight = weight;
        }
    }
}
