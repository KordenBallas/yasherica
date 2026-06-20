using System.Collections.Generic;
using Narrative.Facts.Core;

namespace Narrative.Quests.Core
{
    /// <summary>
    /// Immutable, UnityEngine-free quest fragment (R1): goal structure plus the fact effects written
    /// on completion/failure. Its <see cref="Footprint"/> (the shapes of every effect it can emit) is
    /// the quest's own write-gate (W2-1), so a quest validated and applied after its dialogue session
    /// has ended still checks against the right footprint. Matched into a quest slot by <see cref="Tags"/>.
    /// </summary>
    public sealed class QuestData
    {
        public string QuestId { get; }
        public string DisplayName { get; }
        public string Summary { get; }
        public IReadOnlyList<QuestObjective> Objectives { get; }
        public IReadOnlyList<string> Tags { get; }
        public IReadOnlyList<FactEffectCore> OnCompleteEffects { get; }
        public IReadOnlyList<FactEffectCore> OnFailEffects { get; }

        private readonly List<FactKeyShapeCore> _footprint;

        public QuestData(string questId, string displayName, string summary, IReadOnlyList<QuestObjective> objectives,
            IReadOnlyList<string> tags, IReadOnlyList<FactEffectCore> onCompleteEffects, IReadOnlyList<FactEffectCore> onFailEffects)
        {
            QuestId = questId ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Summary = summary ?? string.Empty;
            Objectives = objectives ?? System.Array.Empty<QuestObjective>();
            Tags = tags ?? System.Array.Empty<string>();
            OnCompleteEffects = onCompleteEffects ?? System.Array.Empty<FactEffectCore>();
            OnFailEffects = onFailEffects ?? System.Array.Empty<FactEffectCore>();
            _footprint = BuildFootprint();
        }

        /// <summary>The union of every fact-write shape this quest can emit (its own write-gate).</summary>
        public IReadOnlyList<FactKeyShapeCore> Footprint => _footprint;

        private List<FactKeyShapeCore> BuildFootprint()
        {
            var shapes = new List<FactKeyShapeCore>();
            AddShapes(shapes, OnCompleteEffects);
            AddShapes(shapes, OnFailEffects);
            foreach (var objective in Objectives)
            {
                AddShapes(shapes, objective.CompletionEffects);
            }

            return shapes;
        }

        private static void AddShapes(List<FactKeyShapeCore> shapes, IReadOnlyList<FactEffectCore> effects)
        {
            for (int i = 0; i < effects.Count; i++)
            {
                var shape = effects[i].Shape;
                if (!shapes.Contains(shape))
                {
                    shapes.Add(shape);
                }
            }
        }
    }
}
