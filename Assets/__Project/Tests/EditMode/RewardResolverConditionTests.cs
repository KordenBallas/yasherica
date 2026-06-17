using CharacterProgression.Core;
using Core.Logging;
using Narrative.Data.Definitions;
using Narrative.Generation;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class RewardResolverConditionTests
    {
        private sealed class NoOpLogger : IGameLogger
        {
            public void Info(string message) { }
            public void Warning(string message) { }
            public void Error(string message) { }
        }

        private static StoryDefinition CreateStoryWithReward(string condition, float probability)
        {
            var reward = ScriptableObject.CreateInstance<RewardDefinition>();

            var story = ScriptableObject.CreateInstance<StoryDefinition>();
            var so = new UnityEditor.SerializedObject(story);
            so.FindProperty("_storyId").stringValue = "s1";

            var rewards = so.FindProperty("_rewards");
            rewards.arraySize = 1;
            var slot = rewards.GetArrayElementAtIndex(0);
            slot.FindPropertyRelative("_reward").objectReferenceValue = reward;
            slot.FindPropertyRelative("_probability").floatValue = probability;
            slot.FindPropertyRelative("_condition").stringValue = condition;
            so.ApplyModifiedPropertiesWithoutUndo();

            return story;
        }

        private static RewardResolver CreateResolver(IRunProgressionRecord record)
        {
            // Probability rolls are deterministic at 1.0 in these tests, so the
            // seed only needs to be stable.
            return new RewardResolver(
                new System.Random(1),
                record,
                new RunConditionEvaluator(new NoOpLogger()));
        }

        [Test]
        public void UnmetCondition_RewardIsSkipped()
        {
            var record = new RunProgressionRecord();
            var story = CreateStoryWithReward("quest_completed:q1", probability: 1f);
            var resolver = CreateResolver(record);

            var result = resolver.Resolve(story);

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void MetCondition_RewardIsResolved()
        {
            var record = new RunProgressionRecord();
            record.CompleteQuest("q1");
            var story = CreateStoryWithReward("quest_completed:q1", probability: 1f);
            var resolver = CreateResolver(record);

            var result = resolver.Resolve(story);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void EmptyCondition_AlwaysResolves()
        {
            var record = new RunProgressionRecord();
            var story = CreateStoryWithReward(string.Empty, probability: 1f);
            var resolver = CreateResolver(record);

            var result = resolver.Resolve(story);

            Assert.AreEqual(1, result.Count);
        }

        [Test]
        public void NoRecord_ConditionGatingDisabled()
        {
            // The seeded-Random-only constructor disables gating: a conditioned
            // slot still resolves (back-compat with existing generation tests).
            var story = CreateStoryWithReward("quest_completed:q1", probability: 1f);
            var resolver = new RewardResolver(new System.Random(1));

            var result = resolver.Resolve(story);

            Assert.AreEqual(1, result.Count);
        }
    }
}
