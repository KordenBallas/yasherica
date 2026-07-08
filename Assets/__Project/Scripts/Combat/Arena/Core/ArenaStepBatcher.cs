using System.Collections.Generic;
using System.Linq;
using Combat.Core;

namespace Combat.Arena.Core
{
    /// <summary>One round's step-major resolution plan: the flattened intents + where each batch ends.</summary>
    public class ArenaBatchedResolution
    {
        public IReadOnlyList<EnemyIntent> Intents { get; }

        /// <summary>Cumulative intent counts at which a batch closes (the last equals the total).</summary>
        public IReadOnlyList<int> BatchEndIndices { get; }

        public ArenaBatchedResolution(
            IReadOnlyList<EnemyIntent> intents, IReadOnlyList<int> batchEndIndices)
        {
            Intents = intents ?? new List<EnemyIntent>();
            BatchEndIndices = batchEndIndices ?? new List<int>();
        }
    }

    /// <summary>
    /// P4-3b step-major ordering: batch k holds every commit's k-th step, intra-batch ordered by
    /// the SAME configured <see cref="IArenaResolutionOrder"/> (each batch is fed to the strategy
    /// as a one-step-per-commit bundle of the same round, so rotation/shuffle apply verbatim).
    /// Only DEATH is simultaneous — moves inside a batch still resolve in order (the same-cell
    /// fizzle rule needs a winner); the win check waits for the batch boundary.
    /// </summary>
    public static class ArenaStepBatcher
    {
        public static ArenaBatchedResolution Batch(ArenaRoundBundle bundle, IArenaResolutionOrder order)
        {
            var intents = new List<EnemyIntent>();
            var boundaries = new List<int>();
            int maxSteps = bundle.Commits.Count == 0 ? 0 : bundle.Commits.Max(c => c.Steps.Count);

            for (int step = 0; step < maxSteps; step++)
            {
                var stepCommits = bundle.Commits
                    .Where(c => c.Steps.Count > step)
                    .Select(c => new ArenaCommit(
                        c.PlayerId, c.UnitId, c.FinalFacing,
                        new List<EnemyIntent> { c.Steps[step] }))
                    .ToList();

                intents.AddRange(order.Order(new ArenaRoundBundle(bundle.RoundNumber, stepCommits)));
                boundaries.Add(intents.Count);
            }

            return new ArenaBatchedResolution(intents, boundaries);
        }
    }
}
