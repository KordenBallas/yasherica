using Combat.Player.AI;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// Proves the profile × difficulty composition rules — in particular that Neutral
    /// difficulty is the identity, which is what keeps legacy profile assets behaving
    /// exactly as before the difficulty layer existed.
    /// </summary>
    [TestFixture]
    public class AITuningTests
    {
        [Test]
        public void NeutralDifficulty_IsTheIdentity()
        {
            var profile = new AIBehaviorProfile(
                damageWeight: 3f, killBonus: 60f, statusEffectBonus: 25f,
                scoreNoise: 4f, pickFromTopN: 2, mistakeChance: 0.1f, aggressionWeight: 1.5f);

            var tuning = AITuning.Compose(profile, AIDifficultySettings.Neutral);

            Assert.AreEqual(profile.DamageWeight, tuning.DamageWeight);
            Assert.AreEqual(profile.KillBonus, tuning.KillBonus);
            Assert.AreEqual(profile.StatusEffectBonus, tuning.StatusEffectBonus);
            Assert.AreEqual(profile.ScoreNoise, tuning.ScoreNoise);
            Assert.AreEqual(profile.PickFromTopN, tuning.PickFromTopN);
            Assert.AreEqual(profile.MistakeChance, tuning.MistakeChance);
            Assert.AreEqual(profile.AggressionWeight, tuning.AggressionWeight);
            Assert.AreEqual(profile.MovementRange, tuning.MovementRange);
        }

        [Test]
        public void QualityDials_ComposeAdditively()
        {
            var profile = new AIBehaviorProfile(scoreNoise: 5f, pickFromTopN: 2, mistakeChance: 0.1f);
            var difficulty = new AIDifficultySettings(
                extraScoreNoise: 20f, extraTopN: 2, extraMistakeChance: 0.15f);

            var tuning = AITuning.Compose(profile, difficulty);

            Assert.AreEqual(25f, tuning.ScoreNoise);
            Assert.AreEqual(4, tuning.PickFromTopN);
            Assert.AreEqual(0.25f, tuning.MistakeChance, 1e-5f);
        }

        [Test]
        public void PriorityScales_ComposeMultiplicatively()
        {
            var profile = new AIBehaviorProfile(
                killBonus: 50f, statusEffectBonus: 30f, aggressionWeight: 2f);
            var difficulty = new AIDifficultySettings(
                aggressionScale: 1.5f, killSecuringScale: 0.5f, statusValueScale: 2f);

            var tuning = AITuning.Compose(profile, difficulty);

            Assert.AreEqual(3f, tuning.AggressionWeight);
            Assert.AreEqual(25f, tuning.KillBonus);
            Assert.AreEqual(60f, tuning.StatusEffectBonus);
        }

        [Test]
        public void MistakeChance_IsClampedToOne_AndTopN_NeverDropsBelowOne()
        {
            var profile = new AIBehaviorProfile(pickFromTopN: 1, mistakeChance: 0.9f);
            var difficulty = new AIDifficultySettings(extraTopN: -5, extraMistakeChance: 0.5f);

            var tuning = AITuning.Compose(profile, difficulty);

            Assert.AreEqual(1f, tuning.MistakeChance);
            Assert.AreEqual(1, tuning.PickFromTopN);
        }
    }
}
