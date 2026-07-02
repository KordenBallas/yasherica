using System.Collections.Generic;
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
        [Header("Variant scoring")]
        [Tooltip("How strongly a part's rarity tier boosts its variant score once unlocked by socketed potency")]
        [Min(0f)]
        [SerializeField] private float _rarityWeight = 0.5f;

        [Header("Socketed Blanks")]
        [Tooltip("How many Part-Blanks the rack holds at once (the multi-track incubation cap)")]
        [Min(1)]
        [SerializeField] private int _blankRackCapacity = 3;

        [Tooltip("How many variant mutations an unsealed blank offers at most")]
        [Min(1)]
        [SerializeField] private int _maxVariantOptions = 3;

        [Tooltip("Socketed target tier required per rarity tier before rare variants are favoured")]
        [Min(0f)]
        [SerializeField] private float _tierUnlockPerRarityTier = 1f;

        [Tooltip("Blanks seeded into the rack at startup (dev seed until blanks drop as loot)")]
        [SerializeField] private List<PartBlankDefinition> _startingBlanks;

        [Header("Card mini-model preview")]
        [SerializeField] private MutationPreviewSettings _preview = new MutationPreviewSettings();

        public float RarityWeight => _rarityWeight;
        public int BlankRackCapacity => _blankRackCapacity;
        public int MaxVariantOptions => _maxVariantOptions;
        public float TierUnlockPerRarityTier => _tierUnlockPerRarityTier;
        public IReadOnlyList<PartBlankDefinition> StartingBlanks =>
            _startingBlanks ?? (IReadOnlyList<PartBlankDefinition>)System.Array.Empty<PartBlankDefinition>();
        public MutationPreviewSettings Preview => _preview ?? (_preview = new MutationPreviewSettings());
    }
}
