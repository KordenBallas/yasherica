using System;
using GameInput.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class PromptCueProviderTests
    {
        private sealed class StubActiveSource : IActiveInputSource
        {
            public InputSource Current { get; private set; } = InputSource.KeyboardMouse;

            public event Action<InputSource> Changed;

            public void Switch(InputSource source)
            {
                Current = source;
                Changed?.Invoke(source);
            }
        }

        [Test]
        public void Cue_FollowsTheActiveSource()
        {
            var source = new StubActiveSource();
            var provider = new PromptCueProvider(new InputBindingCatalog(), source);

            Assert.AreEqual("F", provider.GetCue(GameAction.Interact));

            source.Switch(InputSource.Gamepad);

            Assert.AreEqual("Y", provider.GetCue(GameAction.Interact));
        }

        [Test]
        public void SourceSwitch_RaisesCuesChanged()
        {
            var source = new StubActiveSource();
            var provider = new PromptCueProvider(new InputBindingCatalog(), source);
            int raised = 0;
            provider.CuesChanged += () => raised++;

            source.Switch(InputSource.Gamepad);
            source.Switch(InputSource.Touch);

            Assert.AreEqual(2, raised);
        }

        [Test]
        public void DeferredGap_YieldsEmptyCue_NotAnException()
        {
            var source = new StubActiveSource();
            var provider = new PromptCueProvider(new InputBindingCatalog(), source);
            source.Switch(InputSource.Touch);

            Assert.AreEqual(string.Empty, provider.GetCue(GameAction.Fire));
        }

        [Test]
        public void AbilitySlotCues_MapZeroBasedSlotsToTheSlotActions()
        {
            var source = new StubActiveSource();
            var provider = new PromptCueProvider(new InputBindingCatalog(), source);

            Assert.AreEqual("Q", provider.GetAbilitySlotCue(0));
            Assert.AreEqual("Y", provider.GetAbilitySlotCue(5));
            Assert.AreEqual(string.Empty, provider.GetAbilitySlotCue(6));
            Assert.AreEqual(string.Empty, provider.GetAbilitySlotCue(-1));
        }

        [Test]
        public void Dispose_StopsForwardingSourceChanges()
        {
            var source = new StubActiveSource();
            var provider = new PromptCueProvider(new InputBindingCatalog(), source);
            int raised = 0;
            provider.CuesChanged += () => raised++;

            provider.Dispose();
            source.Switch(InputSource.Gamepad);

            Assert.AreEqual(0, raised);
        }
    }
}
