using System.Collections.Generic;
using Hub.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// O1 cauldron-voice selection: deterministic per (moment, race, salt), race pool preferred
    /// with a generic fallback, quiet on empty pools, and the salt cycles lines across runs.
    /// </summary>
    [TestFixture]
    public class CauldronVoiceTests
    {
        private static CauldronVoiceLines Lines(
            Dictionary<string, IReadOnlyList<string>> byRace = null,
            IReadOnlyList<string> generic = null,
            IReadOnlyList<string> noPart = null,
            IReadOnlyList<string> launch = null,
            IReadOnlyList<string> death = null) =>
            new CauldronVoiceLines(byRace, generic, noPart, launch, death);

        [Test]
        public void SameInputs_SameLine()
        {
            var lines = Lines(launch: new[] { "one", "two", "three" });

            Assert.IsTrue(CauldronVoiceSelector.TrySelect(
                lines, CauldronVoiceMoment.Launch, null, 3, out var first));
            Assert.IsTrue(CauldronVoiceSelector.TrySelect(
                lines, CauldronVoiceMoment.Launch, null, 3, out var second));
            Assert.AreEqual(first, second);
        }

        [Test]
        public void SaltVariation_CyclesThePool()
        {
            var lines = Lines(launch: new[] { "one", "two", "three" });

            var seen = new HashSet<string>();
            for (int salt = 0; salt < 24; salt++)
            {
                Assert.IsTrue(CauldronVoiceSelector.TrySelect(
                    lines, CauldronVoiceMoment.Launch, null, salt, out var line));
                seen.Add(line);
            }

            Assert.Greater(seen.Count, 1, "successive runs must not repeat one line forever");
        }

        [Test]
        public void PartPick_PrefersTheRacePool_FallsBackToGeneric()
        {
            var lines = Lines(
                byRace: new Dictionary<string, IReadOnlyList<string>>
                {
                    ["fox"] = new[] { "fox line" }
                },
                generic: new[] { "generic line" });

            Assert.IsTrue(CauldronVoiceSelector.TrySelect(
                lines, CauldronVoiceMoment.PartPicked, "fox", 1, out var foxLine));
            Assert.AreEqual("fox line", foxLine);

            Assert.IsTrue(CauldronVoiceSelector.TrySelect(
                lines, CauldronVoiceMoment.PartPicked, "ibex", 1, out var fallback));
            Assert.AreEqual("generic line", fallback);

            Assert.IsTrue(CauldronVoiceSelector.TrySelect(
                lines, CauldronVoiceMoment.PartPicked, null, 1, out var kindless));
            Assert.AreEqual("generic line", kindless);
        }

        [Test]
        public void EmptyPools_AreQuiet()
        {
            Assert.IsFalse(CauldronVoiceSelector.TrySelect(
                CauldronVoiceLines.Empty, CauldronVoiceMoment.Launch, null, 1, out _));
            Assert.IsFalse(CauldronVoiceSelector.TrySelect(
                null, CauldronVoiceMoment.Launch, null, 1, out _));
        }

        [Test]
        public void MomentsUseTheirOwnPools()
        {
            var lines = Lines(
                noPart: new[] { "bare" },
                launch: new[] { "go" },
                death: new[] { "back again" });

            CauldronVoiceSelector.TrySelect(lines, CauldronVoiceMoment.NoPartAvailable, null, 1, out var bare);
            CauldronVoiceSelector.TrySelect(lines, CauldronVoiceMoment.Launch, null, 1, out var go);
            CauldronVoiceSelector.TrySelect(lines, CauldronVoiceMoment.DeathReturn, null, 1, out var death);

            Assert.AreEqual("bare", bare);
            Assert.AreEqual("go", go);
            Assert.AreEqual("back again", death);
        }
    }
}
