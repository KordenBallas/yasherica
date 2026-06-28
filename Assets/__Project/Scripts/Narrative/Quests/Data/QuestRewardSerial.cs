using System;
using Narrative.Quests.Core;
using UnityEngine;

namespace Narrative.Quests.Data
{
    /// <summary>
    /// Authoring form of a quest item reward: an artifact definition id and a count. Maps to the
    /// UnityEngine-free <see cref="QuestRewardCore"/>.
    /// </summary>
    [Serializable]
    public class QuestRewardSerial
    {
        [Tooltip("Artifact definition id granted to the inventory on quest completion.")]
        [SerializeField] private string _artifactId = string.Empty;
        [Min(1)]
        [SerializeField] private int _count = 1;

        public QuestRewardCore ToCore() => new QuestRewardCore(_artifactId, _count);
    }
}
