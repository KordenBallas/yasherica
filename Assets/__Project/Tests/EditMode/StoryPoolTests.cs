using System.Collections.Generic;
using LevelGeneration;
using Narrative.Data.Definitions;
using Narrative.Generation;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    [TestFixture]
    public class StoryPoolTests
    {
        private StoryDefinition CreateStory(
            string id,
            LevelTheme[] themes = null,
            int minDiff = 0,
            int maxDiff = 0,
            string[] tags = null,
            bool repeatable = true,
            int cooldown = 0)
        {
            var story = ScriptableObject.CreateInstance<StoryDefinition>();
            // Use serialized field access via SerializedObject for testing
            var so = new UnityEditor.SerializedObject(story);
            so.FindProperty("_storyId").stringValue = id;
            so.FindProperty("_displayName").stringValue = id;
            so.FindProperty("_startingKnot").stringValue = "start";
            so.FindProperty("_minDifficulty").intValue = minDiff;
            so.FindProperty("_maxDifficulty").intValue = maxDiff;
            so.FindProperty("_isRepeatable").boolValue = repeatable;
            so.FindProperty("_cooldownRuns").intValue = cooldown;

            // Create a dummy TextAsset for ink content
            var inkAsset = new TextAsset("{}");
            so.FindProperty("_inkJsonAsset").objectReferenceValue = inkAsset;

            if (themes != null)
            {
                var themeProp = so.FindProperty("_allowedThemes");
                themeProp.arraySize = themes.Length;
                for (int i = 0; i < themes.Length; i++)
                    themeProp.GetArrayElementAtIndex(i).enumValueIndex = (int)themes[i];
            }

            if (tags != null)
            {
                var tagProp = so.FindProperty("_tags");
                tagProp.arraySize = tags.Length;
                for (int i = 0; i < tags.Length; i++)
                    tagProp.GetArrayElementAtIndex(i).stringValue = tags[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return story;
        }

        [Test]
        public void Filter_ReturnsAllStories_WhenNoFiltersApplied()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1"),
                CreateStory("s2"),
                CreateStory("s3")
            };
            var pool = new StoryPool(stories);

            var result = pool.Filter(LevelTheme.Forest, 0, null, null);

            Assert.AreEqual(3, result.Count);
        }

        [Test]
        public void Filter_FiltersByTheme()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1", themes: new[] { LevelTheme.Forest }),
                CreateStory("s2", themes: new[] { LevelTheme.Desert }),
                CreateStory("s3") // No theme restriction = matches all
            };
            var pool = new StoryPool(stories);

            var result = pool.Filter(LevelTheme.Forest, 0, null, null);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("s1", result[0].StoryId);
            Assert.AreEqual("s3", result[1].StoryId);
        }

        [Test]
        public void Filter_FiltersByDifficulty()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1", minDiff: 1, maxDiff: 3),
                CreateStory("s2", minDiff: 4, maxDiff: 6),
                CreateStory("s3") // No difficulty restriction
            };
            var pool = new StoryPool(stories);

            var result = pool.Filter(LevelTheme.Forest, 2, null, null);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("s1", result[0].StoryId);
            Assert.AreEqual("s3", result[1].StoryId);
        }

        [Test]
        public void Filter_FiltersByRequiredTags()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1", tags: new[] { "combat", "dark" }),
                CreateStory("s2", tags: new[] { "peaceful" }),
                CreateStory("s3", tags: new[] { "combat" })
            };
            var pool = new StoryPool(stories);

            var required = new List<string> { "combat" };
            var result = pool.Filter(LevelTheme.Forest, 0, required, null);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("s1", result[0].StoryId);
            Assert.AreEqual("s3", result[1].StoryId);
        }

        [Test]
        public void Filter_ExcludesByTags()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1", tags: new[] { "combat" }),
                CreateStory("s2", tags: new[] { "peaceful" }),
                CreateStory("s3")
            };
            var pool = new StoryPool(stories);

            var excluded = new List<string> { "combat" };
            var result = pool.Filter(LevelTheme.Forest, 0, null, excluded);

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("s2", result[0].StoryId);
            Assert.AreEqual("s3", result[1].StoryId);
        }

        [Test]
        public void Filter_ExcludesStoriesOnCooldown()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1", repeatable: true, cooldown: 2),
                CreateStory("s2")
            };
            var pool = new StoryPool(stories);

            pool.RecordUsage("s1");

            var result = pool.Filter(LevelTheme.Forest, 0, null, null);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("s2", result[0].StoryId);
        }

        [Test]
        public void TickCooldowns_DecrementsAndExpiresCooldowns()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1", repeatable: true, cooldown: 2),
                CreateStory("s2")
            };
            var pool = new StoryPool(stories);

            pool.RecordUsage("s1");

            // After 1 tick, still on cooldown
            pool.TickCooldowns();
            var result1 = pool.Filter(LevelTheme.Forest, 0, null, null);
            Assert.AreEqual(1, result1.Count);

            // After 2nd tick, cooldown expired
            pool.TickCooldowns();
            var result2 = pool.Filter(LevelTheme.Forest, 0, null, null);
            Assert.AreEqual(2, result2.Count);
        }

        [Test]
        public void RecordUsage_NonRepeatableStory_PermanentlyExcluded()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1", repeatable: false),
                CreateStory("s2")
            };
            var pool = new StoryPool(stories);

            pool.RecordUsage("s1");

            // Even after many ticks, non-repeatable stays excluded
            for (int i = 0; i < 100; i++)
                pool.TickCooldowns();

            var result = pool.Filter(LevelTheme.Forest, 0, null, null);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("s2", result[0].StoryId);
        }

        [Test]
        public void Count_ReturnsCorrectTotal()
        {
            var stories = new List<StoryDefinition>
            {
                CreateStory("s1"),
                CreateStory("s2")
            };
            var pool = new StoryPool(stories);

            Assert.AreEqual(2, pool.Count);
        }
    }
}
