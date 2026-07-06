using System.Collections.Generic;
using Narrative.Interaction.Core;
using NUnit.Framework;
using Platform;

namespace Tests.EditMode
{
    /// <summary>
    /// The landing rule (bandit-camp brief req 10, extended to ALL combat content): landing never
    /// starts a fight — activation auto-enters combat only once some enemy on the platform has been
    /// engaged (radius crossed / boss engaged / dialogue combat), so a re-landing resumes a begun
    /// battle and nothing else.
    /// </summary>
    public class CombatAutoStartRuleTests
    {
        private static NpcContent Npc(bool encounterStarted = false, bool isCampBoss = false)
        {
            var npc = new NpcContent(null, null, null, null, NpcIntent.Plain, null, null,
                isCampBoss: isCampBoss);
            npc.EncounterStarted = encounterStarted;
            return npc;
        }

        [Test]
        public void UnengagedEnemies_DoNotAutoStart_LandingIsNeutral()
        {
            var contents = new List<IPlatformContent>
            {
                new EnemyContent { EnemyId = 1 },
                new EnemyContent { EnemyId = 1 }
            };

            Assert.IsFalse(CombatAutoStartRule.ShouldAutoStartCombat(contents));
        }

        [Test]
        public void UnengagedCampCrewBehindABoss_DoesNotAutoStart()
        {
            var contents = new List<IPlatformContent>
            {
                Npc(isCampBoss: true),
                new EnemyContent { EnemyId = 1 },
                new EnemyContent { EnemyId = 1 }
            };

            Assert.IsFalse(CombatAutoStartRule.ShouldAutoStartCombat(contents));
        }

        [Test]
        public void EngagedEnemy_AutoStarts_ReLandingResumesTheBattle()
        {
            var contents = new List<IPlatformContent>
            {
                new EnemyContent { EnemyId = 1, Engaged = true }
            };

            Assert.IsTrue(CombatAutoStartRule.ShouldAutoStartCombat(contents));
        }

        [Test]
        public void EngagedCamp_AutoStarts_EvenWithTheBossContentPresent()
        {
            var contents = new List<IPlatformContent>
            {
                Npc(encounterStarted: true, isCampBoss: true),
                new EnemyContent { EnemyId = 1, Engaged = true },
                new EnemyContent { EnemyId = 2, Engaged = true }
            };

            Assert.IsTrue(CombatAutoStartRule.ShouldAutoStartCombat(contents));
        }

        [Test]
        public void NpcOnly_DoesNotAutoStart()
        {
            var contents = new List<IPlatformContent> { Npc() };

            Assert.IsFalse(CombatAutoStartRule.ShouldAutoStartCombat(contents));
        }

        [Test]
        public void EmptyOrNull_DoesNotAutoStart()
        {
            Assert.IsFalse(CombatAutoStartRule.ShouldAutoStartCombat(new List<IPlatformContent>()));
            Assert.IsFalse(CombatAutoStartRule.ShouldAutoStartCombat(null));
        }
    }
}
