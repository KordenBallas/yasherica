using System.Collections.Generic;
using Narrative.Facts.Core;
using Narrative.Stories.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// The run-level director (R6). MINIMAL this pass: filters storylets whose preconditions all pass
    /// against the shared fact store and picks one via the seeded stream. No pacing or thread-balancing
    /// yet (deferred). Cross-story coupling is purely emergent — the director never reads authored edges
    /// between stories, only facts (R7).
    /// </summary>
    public interface IRunDirector
    {
        StoryletSelection SelectNext(IReadOnlyList<StoryTemplateData> storylets, IFactStore store, ISubjectContext context);
    }
}
