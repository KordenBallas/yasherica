using System;
using System.Collections.Generic;
using Narrative.Facts.Data;
using Narrative.Quests.Core;
using UnityEngine;

namespace Narrative.Quests.Data
{
    /// <summary>
    /// Authoring form of one quest objective. Serialized on <see cref="QuestDefinition"/>.
    /// Configuration data only.
    /// </summary>
    [Serializable]
    public class QuestObjectiveDefinition
    {
        [SerializeField] private string _objectiveId;
        [SerializeField] private string _description;
        [SerializeField] private QuestObjectiveKind _kind = QuestObjectiveKind.Reach;
        [SerializeField] private int _targetCount = 1;
        [Tooltip("Fact effects emitted when this objective completes")]
        [SerializeField] private List<FactEffectSerial> _completionEffects = new List<FactEffectSerial>();

        public string ObjectiveId => _objectiveId;
        public string Description => _description;
        public QuestObjectiveKind Kind => _kind;
        public int TargetCount => _targetCount;
        public IReadOnlyList<FactEffectSerial> CompletionEffects => _completionEffects;
    }
}
