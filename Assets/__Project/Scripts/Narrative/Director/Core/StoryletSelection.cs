using System.Collections.Generic;
using Narrative.Stories.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Result of a director selection pass: the chosen storylet (or null when none are eligible) and
    /// the full eligible set (for the debug view, R13).
    /// </summary>
    public sealed class StoryletSelection
    {
        public StoryTemplateData Chosen { get; }
        public IReadOnlyList<StoryTemplateData> Eligible { get; }

        public StoryletSelection(StoryTemplateData chosen, IReadOnlyList<StoryTemplateData> eligible)
        {
            Chosen = chosen;
            Eligible = eligible ?? System.Array.Empty<StoryTemplateData>();
        }

        public bool HasSelection => Chosen != null;
    }
}
