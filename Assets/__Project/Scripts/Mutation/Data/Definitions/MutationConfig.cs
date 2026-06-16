using UnityEngine;

namespace Mutation.Data.Definitions
{
    /// <summary>
    /// ScriptableObject holding tunables for the mutation subsystem. Contains ONLY configuration
    /// data - NO logic. Loaded from Resources/Mutation/MutationConfig by the MutationInstaller.
    /// </summary>
    [CreateAssetMenu(fileName = "MutationConfig", menuName = "Mutation/Mutation Config")]
    public class MutationConfig : ScriptableObject
    {
        [Header("Digestion")]
        [Tooltip("Artifacts that must be fed in a stage before the character is ready to mutate")]
        [Min(1)]
        [SerializeField] private int _digestionThreshold = 5;

        [Header("Stage-up choice")]
        [Tooltip("How many mutation options to offer at a stage-up (fewer if the dominant archetypes lack parts)")]
        [Min(1)]
        [SerializeField] private int _maxMutationOptions = 3;

        public int DigestionThreshold => _digestionThreshold;
        public int MaxMutationOptions => _maxMutationOptions;
    }
}
