using Combat.Data.Definitions;
using Combat.Player.AI;
using NUnit.Framework;
using UnityEngine;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the SO → Core bridges: a freshly created (legacy-shaped) AIProfileDefinition
    /// maps to perfect decision-quality dials, so old profile assets keep their pre-P2-4
    /// behavior, and a missing DifficultyDefinition maps to the Neutral identity.
    /// The SO-based tests need the Unity engine; on the plain .NET CLI runner
    /// ScriptableObject.CreateInstance throws SecurityException and they no-op.
    /// </summary>
    [TestFixture]
    public class AIDefinitionMapperTests
    {
        [Test]
        public void NullProfileDefinition_MapsToTheDefaultProfile()
        {
            var profile = AIProfileMapper.ToProfile(null);

            Assert.AreEqual(0f, profile.ScoreNoise);
            Assert.AreEqual(1, profile.PickFromTopN);
            Assert.AreEqual(0f, profile.MistakeChance);
            Assert.AreEqual(1f, profile.AggressionWeight);
        }

        [Test]
        public void NullDifficultyDefinition_MapsToNeutral()
        {
            var settings = DifficultyDefinitionMapper.ToSettings(null);

            Assert.AreEqual(0f, settings.ExtraScoreNoise);
            Assert.AreEqual(0, settings.ExtraTopN);
            Assert.AreEqual(0f, settings.ExtraMistakeChance);
            Assert.AreEqual(1f, settings.AggressionScale);
            Assert.AreEqual(1f, settings.KillSecuringScale);
            Assert.AreEqual(1f, settings.StatusValueScale);
        }

        // (f) Back-compat: an asset authored before the new fields existed deserializes with
        // the C# field initializers — perfect quality dials, neutral priorities — and under
        // Neutral difficulty the effective tuning equals the profile.
        [Test]
        public void FreshProfileDefinition_MapsToPerfectQualityDials_AndNeutralIsIdentity()
        {
            AIProfileDefinition definition;
            try
            {
                definition = ScriptableObject.CreateInstance<AIProfileDefinition>();
            }
            catch (System.Security.SecurityException)
            {
                return; // Plain .NET runner: no engine to instantiate SOs — covered in Unity.
            }

            var profile = AIProfileMapper.ToProfile(definition);
            var tuning = AITuning.Compose(profile, AIDifficultySettings.Neutral);

            Assert.AreEqual(0f, profile.ScoreNoise, "legacy assets stay exact");
            Assert.AreEqual(1, profile.PickFromTopN);
            Assert.AreEqual(0f, profile.MistakeChance);
            Assert.AreEqual(3, profile.MovementRange);
            Assert.AreEqual(2.0f, profile.DamageWeight);
            Assert.AreEqual(50f, profile.KillBonus);
            Assert.AreEqual(tuning.KillBonus, profile.KillBonus, "Neutral difficulty is identity");
            Assert.AreEqual(tuning.ScoreNoise, profile.ScoreNoise);

            Object.DestroyImmediate(definition);
        }

        [Test]
        public void FreshDifficultyDefinition_MapsToItsSerializedValues()
        {
            DifficultyDefinition definition;
            try
            {
                definition = ScriptableObject.CreateInstance<DifficultyDefinition>();
            }
            catch (System.Security.SecurityException)
            {
                return; // Plain .NET runner: no engine to instantiate SOs — covered in Unity.
            }

            var settings = DifficultyDefinitionMapper.ToSettings(definition);

            Assert.AreEqual(0f, settings.ExtraScoreNoise, "a fresh asset is Neutral-shaped");
            Assert.AreEqual(1f, settings.AggressionScale);

            Object.DestroyImmediate(definition);
        }
    }
}
