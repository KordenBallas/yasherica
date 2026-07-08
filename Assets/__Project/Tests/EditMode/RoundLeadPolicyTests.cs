using Combat.Core;
using NUnit.Framework;

namespace Tests.EditMode
{
    /// <summary>
    /// The D2 initiative decision: only the opening round is initiator-led — an enemy-initiated fight
    /// resolves enemy intents before the player acts; every other case is player-led.
    /// </summary>
    [TestFixture]
    public class RoundLeadPolicyTests
    {
        [Test]
        public void OpeningRound_EnemyInitiated_EnemyLeads()
        {
            Assert.IsTrue(RoundLeadPolicy.EnemyLeadsThisRound(1, CombatInitiator.Enemy));
        }

        [Test]
        public void OpeningRound_PlayerInitiated_PlayerLeads()
        {
            Assert.IsFalse(RoundLeadPolicy.EnemyLeadsThisRound(1, CombatInitiator.Player));
        }

        [Test]
        public void LaterRound_EnemyInitiated_StillPlayerLeads()
        {
            // Multi-round initiative policy is out of scope: only round 1 is initiator-led.
            Assert.IsFalse(RoundLeadPolicy.EnemyLeadsThisRound(2, CombatInitiator.Enemy));
            Assert.IsFalse(RoundLeadPolicy.EnemyLeadsThisRound(5, CombatInitiator.Enemy));
        }

        [Test]
        public void LaterRound_PlayerInitiated_PlayerLeads()
        {
            Assert.IsFalse(RoundLeadPolicy.EnemyLeadsThisRound(3, CombatInitiator.Player));
        }

        [Test]
        public void EnemiesAlwaysLead_LeadsEveryRound_WhateverTheInitiator()
        {
            // The Heat "they strike first" pact (Track Y).
            Assert.IsTrue(RoundLeadPolicy.EnemyLeadsThisRound(1, CombatInitiator.Player, enemiesAlwaysLead: true));
            Assert.IsTrue(RoundLeadPolicy.EnemyLeadsThisRound(2, CombatInitiator.Player, enemiesAlwaysLead: true));
            Assert.IsTrue(RoundLeadPolicy.EnemyLeadsThisRound(7, CombatInitiator.Enemy, enemiesAlwaysLead: true));
        }

        [Test]
        public void DefaultRule_PreservesTheD2Behaviour()
        {
            // Heat 0 zero-diff: the default argument answers exactly as before Track Y.
            Assert.AreEqual(
                RoundLeadPolicy.EnemyLeadsThisRound(1, CombatInitiator.Enemy),
                RoundLeadPolicy.EnemyLeadsThisRound(1, CombatInitiator.Enemy, enemiesAlwaysLead: false));
            Assert.IsFalse(RoundLeadPolicy.EnemyLeadsThisRound(2, CombatInitiator.Enemy, enemiesAlwaysLead: false));
        }
    }
}
