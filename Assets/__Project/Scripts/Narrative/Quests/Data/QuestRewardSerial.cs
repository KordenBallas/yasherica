using System;
using Narrative.Quests.Core;
using UnityEngine;

namespace Narrative.Quests.Data
{
    /// <summary>
    /// Authoring form of a DECLARED quest reward (P1-5): reward tier + belonging + payload kind —
    /// never a literal item id. The concrete artifact/blank is rolled on completion by the loot
    /// layer. Maps to the UnityEngine-free <see cref="QuestRewardCore"/>.
    /// </summary>
    [Serializable]
    public class QuestRewardSerial
    {
        [Tooltip("What kind of item this reward rolls: a cauldron artifact or a Part-Blank.")]
        [SerializeField] private QuestRewardPayloadKind _payloadKind = QuestRewardPayloadKind.Artifact;

        [Tooltip("Declared potency (the tier the offer card glows with). Same scale as artifact tiers.")]
        [Min(0)]
        [SerializeField] private int _tier;

        [Tooltip("Belonging: a race id for a Part-Blank reward (e.g. 'fox'), a reward-family id for an artifact reward (e.g. 'power'). Empty = unconstrained.")]
        [SerializeField] private string _belongingId = string.Empty;

        public QuestRewardCore ToCore() => new QuestRewardCore(_tier, _belongingId, _payloadKind);
    }
}
