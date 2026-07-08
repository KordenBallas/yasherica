using System.Collections.Generic;
using LevelGeneration;
using LevelGeneration.Journey;
using Loot.Core;
using Narrative.Facts.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class BiomeStretchDirectorTests
    {
        private sealed class FakeThemeProvider : ICurrentThemeProvider
        {
            public readonly List<LevelTheme> SetCalls = new List<LevelTheme>();
            private LevelTheme _theme;
            public LevelTheme CurrentTheme => _theme;

            public void SetTheme(LevelTheme theme)
            {
                _theme = theme;
                SetCalls.Add(theme);
            }
        }

        private sealed class RecordingObserver : IBiomeStretchObserver
        {
            public readonly List<BiomeStretch> Notifications = new List<BiomeStretch>();
            public void OnBiomeStretchChanged(BiomeStretch stretch) => Notifications.Add(stretch);
        }

        /// <summary>Two fixed stretches: Forest tier 1 for windows 0..2, Desert tier 2 from window 3.</summary>
        private sealed class StubJourney : IBiomeJourney
        {
            public BiomeStretch ForWindow(int windowIndex) =>
                windowIndex < 3
                    ? new BiomeStretch(LevelTheme.Forest, 1, 0, 0, 3)
                    : new BiomeStretch(LevelTheme.Desert, 2, 1, 3, 3);
        }

        private FakeThemeProvider _themeProvider;
        private FactStore _facts;
        private RecordingObserver _observer;
        private BiomeStretchDirector _director;

        [SetUp]
        public void SetUp()
        {
            _themeProvider = new FakeThemeProvider();
            _facts = new FactStore();
            _observer = new RecordingObserver();
            _director = new BiomeStretchDirector(new StubJourney(), _themeProvider, _facts, _observer);
        }

        [Test]
        public void FirstWindow_SetsTheme_PublishesTierFact_NotifiesObserver()
        {
            _director.ApplyForWindow(0);

            Assert.AreEqual(LevelTheme.Forest, _themeProvider.CurrentTheme);
            Assert.AreEqual(1, _facts.GetInt(WorldFacts.RunEscalationTier));
            Assert.AreEqual(1, _observer.Notifications.Count);
            Assert.AreEqual(LevelTheme.Forest, _observer.Notifications[0].Theme);
        }

        [Test]
        public void SameStretch_RepeatWindows_NoDuplicateWrites()
        {
            _director.ApplyForWindow(0);
            _director.ApplyForWindow(1);
            _director.ApplyForWindow(2);

            Assert.AreEqual(1, _themeProvider.SetCalls.Count);
            Assert.AreEqual(1, _observer.Notifications.Count);
        }

        [Test]
        public void StretchChange_SwitchesTheme_PublishesNewTier_Notifies()
        {
            _director.ApplyForWindow(0);
            _director.ApplyForWindow(3);

            Assert.AreEqual(LevelTheme.Desert, _themeProvider.CurrentTheme);
            Assert.AreEqual(2, _facts.GetInt(WorldFacts.RunEscalationTier));
            CollectionAssert.AreEqual(
                new[] { LevelTheme.Forest, LevelTheme.Desert }, _themeProvider.SetCalls);
            Assert.AreEqual(2, _observer.Notifications.Count);
        }

        [Test]
        public void NullObserver_IsSafe()
        {
            var director = new BiomeStretchDirector(new StubJourney(), _themeProvider, _facts, observer: null);

            Assert.DoesNotThrow(() =>
            {
                director.ApplyForWindow(0);
                director.ApplyForWindow(3);
            });
            Assert.AreEqual(LevelTheme.Desert, _themeProvider.CurrentTheme);
        }

        [Test]
        public void HeatTierLift_RaisesTheEffectiveTier_AtTheOneWriteSite()
        {
            // Track Y "raised creature floor": authored tier + lift, published once so every
            // consumer (story tier-bands, monster pools, the restore replay) reads one number.
            var director = new BiomeStretchDirector(new StubJourney(), _themeProvider, _facts,
                _observer, logger: null, rules: new JourneyRuleModifiers(2));

            director.ApplyForWindow(0);
            Assert.AreEqual(3, _facts.GetInt(WorldFacts.RunEscalationTier)); // authored 1 + lift 2

            director.ApplyForWindow(3);
            Assert.AreEqual(4, _facts.GetInt(WorldFacts.RunEscalationTier)); // authored 2 + lift 2
        }

        [Test]
        public void NeutralRules_AreAZeroDiff()
        {
            var director = new BiomeStretchDirector(new StubJourney(), _themeProvider, _facts,
                _observer, logger: null, rules: JourneyRuleModifiers.Neutral);

            director.ApplyForWindow(0);

            Assert.AreEqual(1, _facts.GetInt(WorldFacts.RunEscalationTier));
        }
    }
}
