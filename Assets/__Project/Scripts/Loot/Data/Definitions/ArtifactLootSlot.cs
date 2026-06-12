using System;
using UnityEngine;

namespace Loot.Data.Definitions
{
    /// <summary>
    /// One independently-rolled loot slot on an enemy. Configuration data only.
    /// </summary>
    [Serializable]
    public class ArtifactLootSlot
    {
        [Tooltip("ArtifactDefinition.Id this slot yields (e.g. 'fire')")]
        [SerializeField] private string _artifactId;

        [Tooltip("Chance for this slot to drop; each slot rolls independently")]
        [Range(0f, 1f)]
        [SerializeField] private float _probability = 0.5f;

        [Tooltip("Inclusive quantity range dropped when the slot succeeds")]
        [SerializeField] private Vector2Int _quantityRange = new Vector2Int(1, 1);

        [Tooltip("Marks a low-probability, high-value bonus drop")]
        [SerializeField] private bool _isBonus;

        public string ArtifactId => _artifactId;
        public float Probability => _probability;
        public Vector2Int QuantityRange => _quantityRange;
        public bool IsBonus => _isBonus;
    }
}
