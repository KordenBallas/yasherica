using Narrative.Quests.Core;

namespace Loot.Core
{
    /// <summary>The concrete item a declared quest reward rolled into (P1-5).</summary>
    public readonly struct QuestRewardRollResult
    {
        public QuestRewardPayloadKind PayloadKind { get; }
        public string DefinitionId { get; }

        public QuestRewardRollResult(QuestRewardPayloadKind payloadKind, string definitionId)
        {
            PayloadKind = payloadKind;
            DefinitionId = definitionId;
        }
    }

    /// <summary>
    /// Rolls a declared quest reward (tier + belonging + payload kind) into a concrete artifact or
    /// Part-Blank id. Deterministic under the run seed and the given context key; a bias, never a
    /// guaranteed specific piece.
    /// </summary>
    public interface IQuestRewardRoller
    {
        /// <summary>
        /// Rolls the declaration. <paramref name="contextKey"/> must be stable per (quest, reward
        /// index) so re-rolls are impossible and outcomes are order-independent. False when the
        /// relevant pool is empty.
        /// </summary>
        bool TryRoll(QuestRewardCore reward, string contextKey, out QuestRewardRollResult result);
    }
}
