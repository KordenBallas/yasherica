using System.Collections.Generic;
using Narrative.Facts.Data;
using UnityEngine;

namespace Narrative.Quests.Data
{
    /// <summary>
    /// ScriptableObject for a quest fragment (R1): goal structure plus the fact effects written on
    /// completion/failure (its own footprint, W2-1). References no NPC/Dialogue/Enemy; rewards bind by
    /// id through the loot service/effects. Matched into a quest slot by <see cref="QuestTags"/>.
    /// Configuration data only.
    /// </summary>
    [CreateAssetMenu(fileName = "QuestDefinition", menuName = "Narrative/Quests/Quest")]
    public class QuestDefinition : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string _questId;
        [SerializeField] private string _displayName;
        [TextArea]
        [SerializeField] private string _summary;

        [Header("Goal")]
        [SerializeField] private List<QuestObjectiveDefinition> _objectives = new List<QuestObjectiveDefinition>();
        [SerializeField] private List<string> _questTags = new List<string>();

        [Header("Effects (footprint)")]
        [SerializeField] private List<FactEffectSerial> _onCompleteEffects = new List<FactEffectSerial>();
        [SerializeField] private List<FactEffectSerial> _onFailEffects = new List<FactEffectSerial>();

        [Header("Rewards (granted on completion)")]
        [SerializeField] private List<QuestRewardSerial> _rewards = new List<QuestRewardSerial>();

        public string QuestId => _questId;
        public string DisplayName => _displayName;
        public string Summary => _summary;
        public IReadOnlyList<QuestObjectiveDefinition> Objectives => _objectives;
        public IReadOnlyList<string> QuestTags => _questTags;
        public IReadOnlyList<FactEffectSerial> OnCompleteEffects => _onCompleteEffects;
        public IReadOnlyList<FactEffectSerial> OnFailEffects => _onFailEffects;
        public IReadOnlyList<QuestRewardSerial> Rewards => _rewards;
    }
}
