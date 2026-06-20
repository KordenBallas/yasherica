using System.Collections.Generic;
using Narrative.Facts.Core;
using Narrative.Facts.Data;
using Narrative.Quests.Core;

namespace Narrative.Quests.Data
{
    /// <summary>
    /// The only bridge from <see cref="QuestDefinition"/> to the UnityEngine-free <see cref="QuestData"/>
    /// Core record (objectives and effect lists mapped to their Core forms).
    /// </summary>
    public static class QuestMapper
    {
        public static QuestData ToData(QuestDefinition definition)
        {
            if (definition == null)
            {
                return null;
            }

            var objectives = new List<QuestObjective>();
            foreach (var objective in definition.Objectives)
            {
                if (objective == null)
                {
                    continue;
                }

                objectives.Add(new QuestObjective(
                    objective.ObjectiveId,
                    objective.Description,
                    objective.Kind,
                    objective.TargetCount,
                    ToEffects(objective.CompletionEffects)));
            }

            return new QuestData(
                definition.QuestId,
                definition.DisplayName,
                definition.Summary,
                objectives,
                new List<string>(definition.QuestTags),
                ToEffects(definition.OnCompleteEffects),
                ToEffects(definition.OnFailEffects));
        }

        private static List<FactEffectCore> ToEffects(IReadOnlyList<FactEffectSerial> serials)
        {
            var result = new List<FactEffectCore>();
            if (serials == null)
            {
                return result;
            }

            for (int i = 0; i < serials.Count; i++)
            {
                if (serials[i] != null)
                {
                    result.Add(serials[i].ToCore());
                }
            }

            return result;
        }
    }
}
