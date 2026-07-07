using Narrative.Facts.Core;

namespace Narrative.Barks.Core
{
    /// <summary>
    /// Derives the voice's register from the run's existing path facts (P1-10 FR3 — no new meter):
    /// the monster leads only while <c>world.path_conquest</c> strictly exceeds
    /// <c>world.path_restraint</c>; a tie (including the untouched start) reads Restrained, so the
    /// voice begins clipped and must be EARNED into boldness.
    /// </summary>
    public static class PathLeanEvaluator
    {
        public static BarkLean Evaluate(IFactStore facts)
        {
            if (facts == null)
            {
                return BarkLean.Restrained;
            }

            return facts.GetInt(WorldFacts.PathConquest) > facts.GetInt(WorldFacts.PathRestraint)
                ? BarkLean.Indulgent
                : BarkLean.Restrained;
        }
    }
}
