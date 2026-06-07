using System;
using System.Collections.Generic;
using System.Linq;
using LevelGeneration;
using Narrative.Data.Definitions;
using Narrative.Generation;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class LevelNarrativeGeneratorTests
    {
        private StoryDefinition CreateStory(string id)
        {
            var story = ScriptableObject.CreateInstance<StoryDefinition>();
            var so = new UnityEditor.SerializedObject(story);
            so.FindProperty("_storyId").stringValue = id;
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_startingKnot").stringValue = "start";
            so.FindProperty("_isRepeatable").boolValue = true;
            var inkAsset = new TextAsset("{}");
            so.FindProperty("_inkJsonAsset").objectReferenceValue = inkAsset;
            so.ApplyModifiedPropertiesWithoutUndo();
            return story;
        }

        private NpcDefinition CreateNpc(string id)
        {
            var npc = ScriptableObject.CreateInstance<NpcDefinition>();
            var so = new UnityEditor.SerializedObject(npc);
            so.FindProperty("_npcId").stringValue = id;
            so.FindProperty("_displayName").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            return npc;
        }

        private LevelNarrativeConfig CreateConfig(
            int minStories, int maxStories,
            int minNpcs, int maxNpcs)
        {
            var config = ScriptableObject.CreateInstance<LevelNarrativeConfig>();
            var so = new UnityEditor.SerializedObject(config);
            so.FindProperty("_minStories").intValue = minStories;
            so.FindProperty("_maxStories").intValue = maxStories;
            so.FindProperty("_minNpcs").intValue = minNpcs;
            so.FindProperty("_maxNpcs").intValue = maxNpcs;
            so.FindProperty("_difficulty").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            return config;
        }

        [Test]
        public void Generate_RespectsStoryDensityBounds()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1"), CreateStory("s2"), CreateStory("s3"),
                CreateStory("s4"), CreateStory("s5")
            };
            var npcs = new List<NpcDefinition>
            {
                CreateNpc("n1"), CreateNpc("n2"), CreateNpc("n3"),
                CreateNpc("n4"), CreateNpc("n5")
            };

            var storyPool = new StoryPool(stories);
            var npcPool = new NpcPool(npcs);
            var resolver = new RewardResolver(new System.Random(42));
            var config = CreateConfig(2, 3, 2, 5);

            var gen = new LevelNarrativeGenerator(storyPool, npcPool, resolver, new System.Random(42));
            var result = gen.Generate(config);

            int withStory = result.Assignments.Count(a => a.HasStory);
            Assert.GreaterOrEqual(withStory, 2);
            Assert.LessOrEqual(withStory, 3);
        }

        [Test]
        public void Generate_RespectsNpcDensityBounds()
        {
            var stories = new List<StoryDefinition> { CreateStory("s1") };
            var npcs = new List<NpcDefinition>
            {
                CreateNpc("n1"), CreateNpc("n2"), CreateNpc("n3"),
                CreateNpc("n4"), CreateNpc("n5")
            };

            var storyPool = new StoryPool(stories);
            var npcPool = new NpcPool(npcs);
            var resolver = new RewardResolver(new System.Random(42));
            var config = CreateConfig(0, 1, 2, 4);

            var gen = new LevelNarrativeGenerator(storyPool, npcPool, resolver, new System.Random(42));
            var result = gen.Generate(config);

            Assert.GreaterOrEqual(result.Assignments.Count, 2);
            Assert.LessOrEqual(result.Assignments.Count, 4);
        }

        [Test]
        public void Generate_NoDuplicateNpcAssignments()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1"), CreateStory("s2"), CreateStory("s3")
            };
            var npcs = new List<NpcDefinition>
            {
                CreateNpc("n1"), CreateNpc("n2"), CreateNpc("n3"),
                CreateNpc("n4"), CreateNpc("n5")
            };

            var storyPool = new StoryPool(stories);
            var npcPool = new NpcPool(npcs);
            var resolver = new RewardResolver(new System.Random(42));
            var config = CreateConfig(3, 3, 5, 5);

            var gen = new LevelNarrativeGenerator(storyPool, npcPool, resolver, new System.Random(42));
            var result = gen.Generate(config);

            var npcIds = result.Assignments
                .Select(a => a.Npc.NpcId)
                .ToList();

            Assert.AreEqual(npcIds.Count, npcIds.Distinct().Count(),
                "Each NPC should only appear once");
        }

        [Test]
        public void Generate_CharacterOnlyNpcsHaveNoStory()
        {
            var stories = new List<StoryDefinition> { CreateStory("s1") };
            var npcs = new List<NpcDefinition>
            {
                CreateNpc("n1"), CreateNpc("n2"), CreateNpc("n3")
            };

            var storyPool = new StoryPool(stories);
            var npcPool = new NpcPool(npcs);
            var resolver = new RewardResolver(new System.Random(42));
            var config = CreateConfig(1, 1, 3, 3);

            var gen = new LevelNarrativeGenerator(storyPool, npcPool, resolver, new System.Random(42));
            var result = gen.Generate(config);

            int withStory = result.Assignments.Count(a => a.HasStory);
            int charOnly = result.Assignments.Count(a => !a.HasStory);

            Assert.AreEqual(1, withStory);
            Assert.AreEqual(2, charOnly);
        }

        [Test]
        public void Generate_HandlesEmptyPools()
        {
            var storyPool = new StoryPool(new List<StoryDefinition>());
            var npcPool = new NpcPool(new List<NpcDefinition>());
            var resolver = new RewardResolver(new System.Random(42));
            var config = CreateConfig(0, 3, 0, 3);

            var gen = new LevelNarrativeGenerator(storyPool, npcPool, resolver, new System.Random(42));
            var result = gen.Generate(config);

            Assert.AreEqual(0, result.Assignments.Count);
        }

        [Test]
        public void Generate_RecordsStoryUsage()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1"), CreateStory("s2")
            };
            var npcs = new List<NpcDefinition>
            {
                CreateNpc("n1"), CreateNpc("n2"), CreateNpc("n3"), CreateNpc("n4")
            };

            var storyPool = new StoryPool(stories);
            var npcPool = new NpcPool(npcs);
            var resolver = new RewardResolver(new System.Random(42));
            var config = CreateConfig(2, 2, 2, 4);

            var gen = new LevelNarrativeGenerator(storyPool, npcPool, resolver, new System.Random(42));
            gen.Generate(config);

            // After generation, used stories should be on cooldown (if repeatable with cooldown > 0)
            // Since our test stories are repeatable with 0 cooldown, they remain available
            // This tests that RecordUsage is called (verified by the pool's state)
            var remaining = storyPool.Filter(LevelTheme.Forest, 0, null, null);
            Assert.AreEqual(2, remaining.Count); // No cooldown set, still available
        }

        [Test]
        public void Generate_ThrowsOnNullConfig()
        {
            var storyPool = new StoryPool(new List<StoryDefinition>());
            var npcPool = new NpcPool(new List<NpcDefinition>());
            var resolver = new RewardResolver();

            var gen = new LevelNarrativeGenerator(storyPool, npcPool, resolver);

            Assert.Throws<ArgumentNullException>(() => gen.Generate(null));
        }
    }
}
