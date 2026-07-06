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
    }
}
