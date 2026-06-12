using System;
using UnityEngine;

namespace Loot.Data.Definitions
{
    /// <summary>
    /// One weighted entry of a biome loot table. Configuration data only.
    /// </summary>
    [Serializable]
    public class WeightedArtifactEntry
    {
        [Tooltip("ArtifactDefinition.Id this entry yields (e.g. 'fire')")]
        [SerializeField] private string _artifactId;

        [Tooltip("Relative weight for weighted selection")]
        [SerializeField] private float _weight = 1f;

        [Tooltip("When any of these tags matches the roll context tags (story/NPC tags), the weight is multiplied by LootConfig.TagBiasMultiplier")]
        [SerializeField] private string[] _biasTags;

        [Header("Future progression gating (not enforced yet)")]
        [SerializeField] private int _minPlayerLevel;
        [SerializeField] private string[] _requiredAchievements;
        [SerializeField] private string[] _requiredPastQuests;

        public string ArtifactId => _artifactId;
        public float Weight => _weight;
        public string[] BiasTags => _biasTags;
        public int MinPlayerLevel => _minPlayerLevel;
        public string[] RequiredAchievements => _requiredAchievements;
        public string[] RequiredPastQuests => _requiredPastQuests;
    }
}
