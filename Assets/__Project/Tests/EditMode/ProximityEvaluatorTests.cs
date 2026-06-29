using System.Collections.Generic;
using Narrative.Interaction.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ProximityEvaluatorTests
    {
        private readonly ProximityEvaluator _evaluator = new ProximityEvaluator();
        private readonly NpcInteractionSettings _settings = new NpcInteractionSettings(interactionRadius: 3f, aggroRadius: 2f);

        private static PlanarPoint At(float x, float z) => new PlanarPoint(x, z);

        private static NpcProximitySample Npc(string id, float x, float z, NpcIntent intent, bool consumed = false) =>
            new NpcProximitySample(id, At(x, z), intent, consumed);

        [Test]
        public void NearestEligibleInsideInteractionRadius_GetsThePrompt()
        {
            var npcs = new List<NpcProximitySample>
            {
                Npc("far", 2.5f, 0f, NpcIntent.Plain),
                Npc("near", 1f, 0f, NpcIntent.QuestBearer)
            };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.AreEqual("near", result.NearestPromptNpcId);
            Assert.IsEmpty(result.AggroNpcIds);
        }

        [Test]
        public void OutsideInteractionRadius_NoPrompt()
        {
            var npcs = new List<NpcProximitySample> { Npc("plain", 5f, 0f, NpcIntent.Plain) };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.IsNull(result.NearestPromptNpcId);
        }

        [Test]
        public void HostileInsideAggroRadius_IsAggroedAndNeverPrompts()
        {
            var npcs = new List<NpcProximitySample> { Npc("raider", 1.5f, 0f, NpcIntent.Hostile) };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.IsNull(result.NearestPromptNpcId);
            CollectionAssert.AreEqual(new[] { "raider" }, result.AggroNpcIds);
        }

        [Test]
        public void HostileInsideInteractionButOutsideAggro_DoesNothing()
        {
            // 2.5 units: within interaction radius (3) but outside aggro radius (2) — hostiles never prompt.
            var npcs = new List<NpcProximitySample> { Npc("raider", 2.5f, 0f, NpcIntent.Hostile) };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.IsNull(result.NearestPromptNpcId);
            Assert.IsEmpty(result.AggroNpcIds);
        }

        [Test]
        public void ConsumedNpcs_AreIgnored()
        {
            var npcs = new List<NpcProximitySample>
            {
                Npc("talked", 1f, 0f, NpcIntent.QuestBearer, consumed: true),
                Npc("fought", 1f, 0.2f, NpcIntent.Hostile, consumed: true)
            };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.IsNull(result.NearestPromptNpcId);
            Assert.IsEmpty(result.AggroNpcIds);
        }
    }
}
