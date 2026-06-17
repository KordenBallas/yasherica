using System;
using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// One authored affinity of a body part toward a single creature archetype axis: how strongly
    /// this part "belongs to" that archetype (e.g. aquatic 0.2 / reptile 0.8). Configuration data
    /// only; the archetype is referenced by id string (the Mutation layer's ArchetypeDefinition.Id),
    /// so this type takes no dependency on the Mutation layer — mirroring the ArchetypeWeight pattern.
    /// Serialized as a list on <see cref="PartDefinition"/> because Unity cannot serialize a Dictionary;
    /// the mutation part catalog sums duplicate ids, so authoring order is irrelevant.
    /// </summary>
    [Serializable]
    public class ArchetypeAffinity
    {
        [Tooltip("ArchetypeDefinition.Id this part has affinity for (e.g. 'reptile')")]
        [SerializeField] private string _archetypeId;

        [Tooltip("How strongly this part leans toward the archetype (0..1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _weight = 1f;

        public string ArchetypeId => _archetypeId;
        public float Weight => _weight;

        // Parameterless ctor kept for Unity serialization.
        public ArchetypeAffinity()
        {
        }

        // Convenience ctor for programmatic creation and tests.
        public ArchetypeAffinity(string archetypeId, float weight)
        {
            _archetypeId = archetypeId;
            _weight = weight;
        }
    }
}
