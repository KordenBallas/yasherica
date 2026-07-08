using System;
using System.Collections.Generic;
using Combat.Core;
using Combat.Player.AI;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the difficulty filter's contract: perfect dials degenerate to argmax, the
    /// mistake roll abandons the ranking, top-N spreads picks over the best candidates,
    /// and the same seed over the same list always returns the same pick.
    /// </summary>
    [TestFixture]
    public class AIDecisionQualityFilterTests
    {
        private sealed class DummyPlayer : IPlayer
        {
            public int Id => 1;
            public PlayerType Type => PlayerType.AI;
            public string Name => "Dummy";
        }

        private static IReadOnlyList<AIScoredCandidate> ScoredList(params float[] scores)
        {
            var player = new DummyPlayer();
            var list = new List<AIScoredCandidate>();
            for (int i = 0; i < scores.Length; i++)
            {
                // Unit id doubles as the candidate's identity for assertions.
                var candidate = AICandidate.ForEndTurn(new EndUnitTurnAction(player, i));
                list.Add(new AIScoredCandidate(candidate, scores[i]));
            }
            return list;
        }

        private static int PickedIndex(AICandidate picked)
        {
            return ((EndUnitTurnAction)picked.Action).UnitId;
        }

        private static AITuning Tuning(
            float scoreNoise = 0f, int pickFromTopN = 1, float mistakeChance = 0f)
        {
            return AITuning.Compose(
                new AIBehaviorProfile(
                    scoreNoise: scoreNoise, pickFromTopN: pickFromTopN, mistakeChance: mistakeChance),
                AIDifficultySettings.Neutral);
        }

        [Test]
        public void PerfectDials_AlwaysReturnTheArgmax()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var filter = new AIDecisionQualityFilter(new Random(seed));
                var picked = filter.Pick(ScoredList(5f, 42f, 17f), Tuning());
                Assert.AreEqual(1, PickedIndex(picked), $"seed {seed}");
            }
        }

        [Test]
        public void CertainMistake_PicksBeyondTheArgmax()
        {
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 50; seed++)
            {
                var filter = new AIDecisionQualityFilter(new Random(seed));
                seen.Add(PickedIndex(filter.Pick(ScoredList(5f, 42f, 17f), Tuning(mistakeChance: 1f))));
            }

            Assert.Greater(seen.Count, 1, "a certain mistake must not collapse to one pick");
        }

        [Test]
        public void TopN_SpreadsPicksOverTheBestCandidates_AndNeverReachesTheWorst()
        {
            var seen = new HashSet<int>();
            for (int seed = 0; seed < 100; seed++)
            {
                var filter = new AIDecisionQualityFilter(new Random(seed));
                seen.Add(PickedIndex(filter.Pick(ScoredList(30f, 42f, 17f, 1f), Tuning(pickFromTopN: 3))));
            }

            Assert.IsTrue(seen.Contains(1) && seen.Contains(0), "top-3 reaches the runners-up");
            Assert.IsFalse(seen.Contains(3), "the worst candidate is outside top-3");
        }

        [Test]
        public void SameSeed_SameList_SamePick_WithNoiseAndTopN()
        {
            for (int seed = 0; seed < 20; seed++)
            {
                var first = new AIDecisionQualityFilter(new Random(seed))
                    .Pick(ScoredList(30f, 42f, 17f, 40f), Tuning(scoreNoise: 10f, pickFromTopN: 3));
                var second = new AIDecisionQualityFilter(new Random(seed))
                    .Pick(ScoredList(30f, 42f, 17f, 40f), Tuning(scoreNoise: 10f, pickFromTopN: 3));

                Assert.AreEqual(PickedIndex(first), PickedIndex(second), $"seed {seed}");
            }
        }

        [Test]
        public void EqualScores_KeepEnumerationOrder_UnderPerfectDials()
        {
            var filter = new AIDecisionQualityFilter(new Random(1));
            var picked = filter.Pick(ScoredList(42f, 42f, 42f), Tuning());

            Assert.AreEqual(0, PickedIndex(picked), "stable sort keeps the first equal candidate on top");
        }
    }
}
