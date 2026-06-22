using Narrative.Facts.Core;

namespace Narrative.Director.Core
{
    /// <summary>
    /// Plans one window of platforms story-first (R5): selects a budgeted set of eligible stories,
    /// matches an actor to each, and pads to the window size. Selection reads the live fact store at
    /// plan time (R6/R7), so a window reflects every choice made up to the moment it is planned.
    /// </summary>
    public interface IRunWindowPlanner
    {
        WindowPlan PlanWindow(int windowIndex, IFactStore facts);
    }
}
