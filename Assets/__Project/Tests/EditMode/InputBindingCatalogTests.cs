using GameInput.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class InputBindingCatalogTests
    {
        private readonly InputBindingCatalog _catalog = new InputBindingCatalog();

        [Test]
        public void InteractCue_PerSource_MatchesTheDeviceVocabulary()
        {
            Assert.AreEqual("F", _catalog.GetCue(GameAction.Interact, InputSource.KeyboardMouse));
            Assert.AreEqual("Y", _catalog.GetCue(GameAction.Interact, InputSource.Gamepad));
            Assert.AreEqual("Tap", _catalog.GetCue(GameAction.Interact, InputSource.Touch));
        }

        [Test]
        public void UnboundPair_ReturnsNullCue()
        {
            Assert.IsNull(_catalog.GetCue(GameAction.Fire, InputSource.Touch));
            Assert.IsFalse(_catalog.IsBound(GameAction.Fire, InputSource.Touch));
        }

        [Test]
        public void EveryEntry_HasANonEmptyCue()
        {
            foreach (var entry in _catalog.Entries)
            {
                Assert.IsNotEmpty(entry.Cue, $"{entry.Action} × {entry.Source} has an empty cue");
            }
        }

        [Test]
        public void KeyboardAndGamepadEntries_CarryControlPaths_TouchRowsAreOverlayBacked()
        {
            foreach (var entry in _catalog.Entries)
            {
                if (entry.Source == InputSource.Touch)
                {
                    Assert.IsFalse(entry.HasControlPath,
                        $"{entry.Action} × Touch should be overlay/pointer-backed, not a device binding");
                }
                else
                {
                    Assert.IsTrue(entry.HasControlPath,
                        $"{entry.Action} × {entry.Source} must name the control path the actions asset binds");
                }
            }
        }

        [Test]
        public void NoDuplicateActionSourcePairs()
        {
            var entries = _catalog.Entries;
            for (int i = 0; i < entries.Count; i++)
            {
                for (int j = i + 1; j < entries.Count; j++)
                {
                    Assert.IsFalse(
                        entries[i].Action == entries[j].Action && entries[i].Source == entries[j].Source,
                        $"Duplicate catalog row for {entries[i].Action} × {entries[i].Source}");
                }
            }
        }

        [Test]
        public void EveryDeferredGap_NamesReasonAndRoadmapReference()
        {
            foreach (var gap in _catalog.DeferredGaps)
            {
                Assert.IsNotEmpty(gap.Reason, $"{gap.Action} × {gap.Source} gap has no reason");
                Assert.IsNotEmpty(gap.RoadmapReference, $"{gap.Action} × {gap.Source} gap has no roadmap reference");
            }
        }
    }
}
