using Core.Logging;

namespace CharacterProgression.Core
{
    /// <summary>
    /// Evaluates a single-predicate condition string against an
    /// <see cref="IRunProgressionRecord"/>. Used to gate run-state-dependent
    /// content (e.g. <c>RewardSlot.Condition</c>).
    ///
    /// Grammar (one predicate):
    /// <list type="bullet">
    /// <item>empty / null -> passes</item>
    /// <item><c>quest_completed:&lt;id&gt;</c> / <c>quest_active:&lt;id&gt;</c> / <c>quest_failed:&lt;id&gt;</c></item>
    /// <item><c>npc_encountered:&lt;id&gt;</c></item>
    /// </list>
    /// An unrecognized or malformed condition fails closed (returns false) and
    /// logs a warning, so authoring errors surface and never over-grant.
    /// Boolean composition (AND/OR) is intentionally not supported yet.
    /// Pure C#, no UnityEngine.
    /// </summary>
    public class RunConditionEvaluator
    {
        private const char Separator = ':';

        private const string QuestCompleted = "quest_completed";
        private const string QuestActive = "quest_active";
        private const string QuestFailed = "quest_failed";
        private const string NpcEncountered = "npc_encountered";

        private readonly IGameLogger _logger;

        public RunConditionEvaluator(IGameLogger logger)
        {
            _logger = logger;
        }

        public bool Evaluate(string condition, IRunProgressionRecord record)
        {
            if (string.IsNullOrWhiteSpace(condition))
                return true;

            var trimmed = condition.Trim();
            var separatorIndex = trimmed.IndexOf(Separator);
            if (separatorIndex <= 0 || separatorIndex >= trimmed.Length - 1)
            {
                _logger?.Warning($"[RunConditionEvaluator] Malformed condition '{condition}' - failing closed.");
                return false;
            }

            var prefix = trimmed.Substring(0, separatorIndex).Trim();
            var id = trimmed.Substring(separatorIndex + 1).Trim();

            switch (prefix)
            {
                case QuestCompleted:
                    return record.IsQuestCompleted(id);
                case QuestActive:
                    return record.IsQuestActive(id);
                case QuestFailed:
                    return record.IsQuestFailed(id);
                case NpcEncountered:
                    return record.HasEncounteredNpc(id);
                default:
                    _logger?.Warning($"[RunConditionEvaluator] Unknown condition prefix '{prefix}' in '{condition}' - failing closed.");
                    return false;
            }
        }
    }
}
