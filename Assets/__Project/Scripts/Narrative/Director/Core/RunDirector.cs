using System.Collections.Generic;
using Narrative.Facts.Core;
using Narrative.Stories.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Default <see cref="IRunDirector"/>. Eligibility = all preconditions pass over the shared store
    /// (uniformly across world/actor/faction, R9). Among eligible storylets, picks one with the seeded
    /// PRNG so selection is deterministic and save-replayable (B2). Candidates are considered in a stable
    /// id order before the seeded pick.
    /// </summary>
    public sealed class RunDirector : IRunDirector
    {
        private readonly IPreconditionEvaluator _evaluator;
        private readonly IRandomSource _random;

        public RunDirector(IPreconditionEvaluator evaluator, IRandomSource random)
        {
            _evaluator = evaluator;
            _random = random;
        }

        public StoryletSelection SelectNext(IReadOnlyList<StoryTemplateData> storylets, IFactStore store, ISubjectContext context)
        {
            var eligible = new List<StoryTemplateData>();
            if (storylets != null)
            {
                foreach (var story in storylets)
                {
                    if (story != null && _evaluator.EvaluateAll(story.Preconditions, store, context))
                    {
                        eligible.Add(story);
                    }
                }
            }

            eligible.Sort((a, b) => string.CompareOrdinal(a.StoryId, b.StoryId));

            StoryTemplateData chosen = null;
            if (eligible.Count == 1)
            {
                chosen = eligible[0];
            }
            else if (eligible.Count > 1)
            {
                chosen = eligible[_random.NextInt(eligible.Count)];
            }

            return new StoryletSelection(chosen, eligible);
        }
    }
}
