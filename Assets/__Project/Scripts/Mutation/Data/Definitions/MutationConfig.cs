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
        [Tooltip("How many mutation options to offer at a stage-up (fewer if too few parts score positively)")]
        [Min(1)]
        [SerializeField] private int _maxMutationOptions = 3;

        [Header("Scoring")]
        [Tooltip("How strongly a part's rarity tier boosts its score once unlocked by accumulated points")]
        [Min(0f)]
        [SerializeField] private float _rarityWeight = 0.5f;

        [Tooltip("Accumulated archetype points required per rarity tier before that tier is favoured")]
        [Min(0f)]
        [SerializeField] private float _rarityUnlockPointsPerTier = 10f;

        public int DigestionThreshold => _digestionThreshold;
        public int MaxMutationOptions => _maxMutationOptions;
        public float RarityWeight => _rarityWeight;
        public float RarityUnlockPointsPerTier => _rarityUnlockPointsPerTier;
    }
}
