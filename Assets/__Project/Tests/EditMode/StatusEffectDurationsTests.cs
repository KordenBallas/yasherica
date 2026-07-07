using System.Collections.Generic;
using Combat.Core;
using Combat.Core.StatusEffects;
using NUnit.Framework;

namespace Tests.EditMode
{
    [TestFixture]
    public class StatusEffectDurationsTests
    {
        private static IStatusEffect Effect(int duration)
        {
            return new StatusEffect(1, "test", StatusEffectType.Buff, duration);
        }

        private static IReadOnlyList<IStatusEffect> List(params IStatusEffect[] effects)
        {
            return effects;
        }

        [Test]
        public void Tick_PositiveDuration_DecrementsByOne()
        {
            var result = StatusEffectDurations.Tick(List(Effect(3)));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(2, result[0].Duration);
        }

        [Test]
        public void Tick_DurationOfOne_IsRemovedWhenItReachesZero()
        {
            var result = StatusEffectDurations.Tick(List(Effect(1)));

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void Tick_ZeroDuration_IsRemoved()
        {
            var result = StatusEffectDurations.Tick(List(Effect(0)));

            CollectionAssert.IsEmpty(result);
        }

        [Test]
        public void Tick_NegativeDuration_IsPermanent()
        {
            var effects = List(Effect(-1));

            // Even after many turns the infinite effect persists and keeps its duration.
            for (var turn = 0; turn < 5; turn++)
            {
                effects = StatusEffectDurations.Tick(effects);
            }

            Assert.AreEqual(1, effects.Count);
            Assert.AreEqual(-1, effects[0].Duration);
        }

        [Test]
        public void Tick_MixedEffects_DecrementsTicking_KeepsInfinite_DropsExpired()
        {
            var result = StatusEffectDurations.Tick(List(Effect(2), Effect(-1), Effect(1)));

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(1, result[0].Duration);  // 2 -> 1
            Assert.AreEqual(-1, result[1].Duration); // infinite untouched
            // the duration-1 effect ticked to 0 and was dropped
        }

        [Test]
        public void Tick_NullInput_ReturnsEmpty()
        {
            CollectionAssert.IsEmpty(StatusEffectDurations.Tick(null));
        }

        [Test]
        public void WithDuration_ResetsTheClock_PreservingIdentityAndStacks()
        {
            var effect = new StatusEffect(1, "test", StatusEffectType.Buff, 1, stackCount: 2,
                stackRule: StackRule.StackToCap);

            var refreshed = effect.WithDuration(3);

            Assert.AreEqual(3, refreshed.Duration);
            Assert.AreEqual(2, refreshed.StackCount);
            Assert.AreEqual(effect.Id, refreshed.Id);
            Assert.AreEqual(StackRule.StackToCap, refreshed.StackRule);
        }
    }
}
