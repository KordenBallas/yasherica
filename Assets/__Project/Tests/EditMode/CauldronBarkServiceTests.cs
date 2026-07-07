using System.Collections.Generic;
using Narrative.Barks.Core;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The cauldron's live bark channel (P1-10, cauldron-voice-barks.md): slot × lean pools,
    /// path-reactive register off the two path counters (no meter), deterministic selection under
    /// the run seed, and the data-authored dark-belonging vocabulary.
    /// </summary>
    [TestFixture]
    public class CauldronBarkServiceTests
    {
        private static FactStore Store() => new FactStore(new FactKeyRegistry(new[]
        {
            new FactKeyInfo(FactNamespace.World, "path_conquest", FactScope.Global, FactValueType.Int, FactValue.FromInt(0)),
            new FactKeyInfo(FactNamespace.World, "path_restraint", FactScope.Global, FactValueType.Int, FactValue.FromInt(0))
        }), null);

        private static CauldronBarkLines Lines() => new CauldronBarkLines(
            new Dictionary<(CauldronBarkSlot, BarkLean), IReadOnlyList<string>>
            {
                { (CauldronBarkSlot.Temptation, BarkLean.Indulgent), new[] { "bold-a", "bold-b" } },
                { (CauldronBarkSlot.Temptation, BarkLean.Restrained), new[] { "sour-a", "sour-b" } },
                { (CauldronBarkSlot.DarkOffer, BarkLean.Indulgent), new[] { "lean-in" } }
                // DarkOffer has no restrained pool: falls back to the indulgent one.
                // Restraint has nothing at all: a quiet slot.
            },
            new[] { "power" });

        private static string BarkOnce(CauldronBarkService service, CauldronBarkSlot slot)
        {
            string spoken = null;
            service.OnBark += line => spoken = line;
            service.Bark(slot);
            return spoken;
        }

        [Test]
        public void Lean_TracksThePathCounters_SameSlotChangesRegister()
        {
            var store = Store();
            var service = new CauldronBarkService(Lines(), store, runSeed: 5);

            var neutral = BarkOnce(service, CauldronBarkSlot.Temptation);
            StringAssert.StartsWith("sour", neutral, "an untouched run reads Restrained (boldness is earned)");

            store.SetInt(WorldFacts.PathConquest, 3);
            var indulged = BarkOnce(service, CauldronBarkSlot.Temptation);
            StringAssert.StartsWith("bold", indulged, "the monster leads - the voice grows bold");

            store.SetInt(WorldFacts.PathRestraint, 4);
            var restrained = BarkOnce(service, CauldronBarkSlot.Temptation);
            StringAssert.StartsWith("sour", restrained, "restraint retakes the lead - the voice sours");
        }

        [Test]
        public void SameSeed_SpeaksTheSameSequence()
        {
            var a = new CauldronBarkService(Lines(), Store(), runSeed: 77);
            var b = new CauldronBarkService(Lines(), Store(), runSeed: 77);

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(BarkOnce(a, CauldronBarkSlot.Temptation),
                    BarkOnce(b, CauldronBarkSlot.Temptation), $"fire {i} must replay identically");
            }
        }

        [Test]
        public void ConsecutiveFires_CycleThePool_NoBackToBackRepeat()
        {
            var service = new CauldronBarkService(Lines(), Store(), runSeed: 9);

            var first = BarkOnce(service, CauldronBarkSlot.Temptation);
            var second = BarkOnce(service, CauldronBarkSlot.Temptation);

            Assert.AreNotEqual(first, second, "a two-line pool never repeats back-to-back");
        }

        [Test]
        public void MissingLeanPool_FallsBackToTheOtherRegister()
        {
            var service = new CauldronBarkService(Lines(), Store(), runSeed: 1);

            // The run is neutral => Restrained, but DarkOffer only authored an indulgent pool.
            Assert.AreEqual("lean-in", BarkOnce(service, CauldronBarkSlot.DarkOffer));
        }

        [Test]
        public void UnauthoredSlot_StaysQuiet()
        {
            var service = new CauldronBarkService(Lines(), Store(), runSeed: 1);

            Assert.IsNull(BarkOnce(service, CauldronBarkSlot.Restraint));
        }

        [Test]
        public void DarkBelonging_IsTheAuthoredVocabulary()
        {
            var service = new CauldronBarkService(Lines(), Store(), runSeed: 1);

            Assert.IsTrue(service.IsDarkBelonging("power"));
            Assert.IsFalse(service.IsDarkBelonging("utility"));
            Assert.IsFalse(service.IsDarkBelonging(""));
            Assert.IsFalse(service.IsDarkBelonging(null));
        }
    }
}
