using System;
using UnityEngine;

namespace CharacterSystem.Data.Definitions
{
    /// <summary>
    /// One authored affinity of a body part toward a single function trait: how strongly this part
    /// expresses that trait as an unseal variant (e.g. sharp 0.7 / chitin 0.5). Configuration data
    /// only; the trait is referenced by id string (the Inventory layer's TraitDefinition.Id), so
    /// this type takes no dependency on the Inventory layer. Serialized as a list on
    /// <see cref="PartDefinition"/> because Unity cannot serialize a Dictionary; the mutation part
    /// catalog sums duplicate ids, so authoring order is irrelevant.
    /// </summary>
    [Serializable]
    public class TraitAffinity
    {
        [Tooltip("TraitDefinition.Id this part expresses (e.g. 'sharp')")]
        [SerializeField] private string _traitId;

        [Tooltip("How strongly this part expresses the trait (0..1)")]
        [Range(0f, 1f)]
        [SerializeField] private float _weight = 1f;

        public string TraitId => _traitId;
        public float Weight => _weight;

        // Parameterless ctor kept for Unity serialization.
        public TraitAffinity()
        {
        }

        // Convenience ctor for programmatic creation and tests.
        public TraitAffinity(string traitId, float weight)
        {
            _traitId = traitId;
            _weight = weight;
        }
    }
}
