using Narrative.Director.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class DeterministicRandomTests
    {
        [Test]
        public void SameSeed_ProducesSameSequence()
        {
            var a = new DeterministicRandom(12345);
            var b = new DeterministicRandom(12345);
            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(a.NextInt(1000), b.NextInt(1000));
            }
        }

        [Test]
        public void CapturedState_ReplaysSubsequentDraws()
        {
            // B2: capture state mid-stream, continue on a fresh instance restored to that state.
            var rng = new DeterministicRandom(999);
            rng.NextInt(100);
            rng.NextInt(100);
            ulong saved = rng.State;

            var expected = new[] { rng.NextInt(100), rng.NextInt(100), rng.NextInt(100) };

            var restored = new DeterministicRandom(0) { State = saved };
            Assert.AreEqual(expected[0], restored.NextInt(100));
            Assert.AreEqual(expected[1], restored.NextInt(100));
            Assert.AreEqual(expected[2], restored.NextInt(100));
        }

        [Test]
        public void NextInt_RespectsBoundsAndDegenerateCases()
        {
            var rng = new DeterministicRandom(1);
            Assert.AreEqual(0, rng.NextInt(1));
            Assert.AreEqual(0, rng.NextInt(0));
            for (int i = 0; i < 50; i++)
            {
                int v = rng.NextInt(5);
                Assert.GreaterOrEqual(v, 0);
                Assert.Less(v, 5);
            }
        }
    }
}
