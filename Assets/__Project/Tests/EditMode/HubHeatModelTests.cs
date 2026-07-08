using Heat.Core;
using Hub.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class HubHeatModelTests
    {
        [Test]
        public void CycleRank_WalksUpThenWrapsToZero()
        {
            var model = new HubHeatModel(HeatPactTests.DemoSettings());

            Assert.AreEqual(1, model.CycleRank("raised-floor"));
            Assert.AreEqual(2, model.CycleRank("raised-floor"));
            Assert.AreEqual(0, model.CycleRank("raised-floor"), "past max wraps to zero");
        }

        [Test]
        public void TotalHeat_TracksTheDialedRanks()
        {
            var model = new HubHeatModel(HeatPactTests.DemoSettings());
            model.CycleRank("raised-floor"); // +2
            model.CycleRank("stingy-cauldron"); // +1

            Assert.AreEqual(3, model.TotalHeat);
        }

        [Test]
        public void SoftCap_WrapsAnUnaffordableStepToZero()
        {
            // Cap 5: enemies-first (2) + raised-floor rank 2 (4) would be 6 — the raised-floor
            // second step wraps to 0 instead, so the modifier stays cyclable and the cap holds.
            var model = new HubHeatModel(HeatPactTests.DemoSettings(softCap: 5));
            model.CycleRank("enemies-first"); // total 2
            model.CycleRank("raised-floor"); // total 4

            Assert.AreEqual(0, model.CycleRank("raised-floor"), "step to rank 2 (total 6) wraps to 0");
            Assert.AreEqual(2, model.TotalHeat);
        }

        [Test]
        public void NextStepIsCapped_MarksTheRefusal_SoItIsNeverSilent()
        {
            // The 2026-07-08 playtest bug: an unaffordable step did nothing with no feedback.
            var model = new HubHeatModel(HeatPactTests.DemoSettings(softCap: 5));
            model.CycleRank("enemies-first"); // total 2
            model.CycleRank("raised-floor"); // total 4

            Assert.IsTrue(model.NextStepIsCapped("raised-floor"), "rank 2 (total 6) is over cap 5");
            Assert.IsTrue(model.NextStepIsCapped("fewer-sockets"), "rank 1 (total 6) is over cap 5");
            Assert.IsFalse(model.NextStepIsCapped("stingy-cauldron"), "rank 1 (total 5) still fits");
            Assert.IsFalse(model.NextStepIsCapped("enemies-first"),
                "at max rank the next step is the wrap to 0 — always allowed, never capped");
            Assert.IsFalse(new HubHeatModel(HeatPactTests.DemoSettings(softCap: 0))
                .NextStepIsCapped("raised-floor"), "cap 0 = uncapped");
        }

        [Test]
        public void SoftCap_ExposedForTheSealCardReadout()
        {
            Assert.AreEqual(5, new HubHeatModel(HeatPactTests.DemoSettings(softCap: 5)).SoftCap);
            Assert.AreEqual(0, new HubHeatModel(HeatPactTests.DemoSettings()).SoftCap);
        }

        [Test]
        public void Events_FireOnChangeOpenAndSeal()
        {
            var model = new HubHeatModel(HeatPactTests.DemoSettings());
            int changed = 0, opened = 0, sealedTotal = -1;
            model.PactChanged += () => changed++;
            model.Opened += () => opened++;
            model.PactSealed += total => sealedTotal = total;

            model.NotifyOpened();
            model.CycleRank("enemies-first");
            model.CycleRank("unknown-id"); // no-op: no event
            model.Seal();

            Assert.AreEqual(1, opened);
            Assert.AreEqual(1, changed);
            Assert.AreEqual(2, sealedTotal);
        }

        [Test]
        public void BuildPact_MatchesTheDialedState()
        {
            var model = new HubHeatModel(HeatPactTests.DemoSettings());
            model.CycleRank("fewer-sockets");

            var pact = model.BuildPact();

            Assert.AreEqual(1, pact.RankOf("fewer-sockets"));
            Assert.AreEqual(model.TotalHeat, pact.TotalHeat);
        }

        [Test]
        public void EmptyMenu_IsInert()
        {
            var model = new HubHeatModel(HeatSettings.Defaults);

            Assert.AreEqual(0, model.Menu.Count);
            Assert.AreEqual(0, model.CycleRank("anything"));
            Assert.AreEqual(0, model.TotalHeat);
        }
    }
}
