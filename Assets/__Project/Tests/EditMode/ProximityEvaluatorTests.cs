using System.Collections.Generic;
using Narrative.Interaction.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class ProximityEvaluatorTests
    {
        private readonly ProximityEvaluator _evaluator = new ProximityEvaluator();
        private readonly NpcInteractionSettings _settings = new NpcInteractionSettings(
            interactionRadius: 3f, aggroRadius: 2f, bossEngagementRadius: 6f);

        private static PlanarPoint At(float x, float z) => new PlanarPoint(x, z);

        private static NpcProximitySample Npc(string id, float x, float z, NpcIntent intent, bool consumed = false,
            bool isBoss = false, bool onPlayerPlatform = true) =>
            new NpcProximitySample(id, At(x, z), intent, consumed, isBoss, onPlayerPlatform);

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

        // --- Camp boss engagement (bandit-camp brief reqs 7-9) ---

        [Test]
        public void HostileBoss_EngagesAtTheLargerRadius()
        {
            // 5.9 units: far beyond the normal aggro radius (2), inside the boss radius (6).
            var npcs = new List<NpcProximitySample> { Npc("boss", 5.9f, 0f, NpcIntent.Hostile, isBoss: true) };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            CollectionAssert.AreEqual(new[] { "boss" }, result.AggroNpcIds);
        }

        [Test]
        public void HostileBoss_OutsideBossRadius_DoesNothing()
        {
            var npcs = new List<NpcProximitySample> { Npc("boss", 6.1f, 0f, NpcIntent.Hostile, isBoss: true) };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.IsEmpty(result.AggroNpcIds);
            Assert.IsEmpty(result.AutoTalkNpcIds);
        }

        [Test]
        public void QuestBearerBoss_AutoTalksInsteadOfPrompting()
        {
            var npcs = new List<NpcProximitySample> { Npc("boss", 5f, 0f, NpcIntent.QuestBearer, isBoss: true) };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.IsNull(result.NearestPromptNpcId);
            CollectionAssert.AreEqual(new[] { "boss" }, result.AutoTalkNpcIds);
        }

        [Test]
        public void ConsumedBoss_DoesNotReEngage()
        {
            var npcs = new List<NpcProximitySample>
            {
                Npc("boss", 1f, 0f, NpcIntent.QuestBearer, consumed: true, isBoss: true)
            };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.IsEmpty(result.AutoTalkNpcIds);
        }

        // --- Platform scoping (engagement is a local, on-platform act) ---

        [Test]
        public void OffPlatformNpcs_AreIneligibleAtAnyDistance()
        {
            var npcs = new List<NpcProximitySample>
            {
                Npc("boss", 1f, 0f, NpcIntent.Hostile, isBoss: true, onPlayerPlatform: false),
                Npc("raider", 0.5f, 0f, NpcIntent.Hostile, onPlayerPlatform: false),
                Npc("villager", 0.5f, 0.2f, NpcIntent.Plain, onPlayerPlatform: false)
            };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.IsNull(result.NearestPromptNpcId);
            Assert.IsEmpty(result.AggroNpcIds);
            Assert.IsEmpty(result.AutoTalkNpcIds);
        }

        [Test]
        public void OnPlatformNpc_StillEngages()
        {
            var npcs = new List<NpcProximitySample>
            {
                Npc("off", 1f, 0f, NpcIntent.QuestBearer, onPlayerPlatform: false),
                Npc("on", 2f, 0f, NpcIntent.QuestBearer)
            };

            var result = _evaluator.Evaluate(At(0f, 0f), npcs, _settings);

            Assert.AreEqual("on", result.NearestPromptNpcId);
        }
    }
}
