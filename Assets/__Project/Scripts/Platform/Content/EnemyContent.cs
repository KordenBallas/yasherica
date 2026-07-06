using Combat.Enemy;
using Combat.Core;
using Core.Logging;
using UnityEngine;

namespace Platform
{
    public class EnemyContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Enemy;

        public int EnemyId { get; set; }

        /// <summary>
        /// Latched when this platform's fight is triggered (an enemy's aggro radius crossed, the camp
        /// boss engaged, or a dialogue combat branch). Landing on a platform NEVER starts combat —
        /// <see cref="CombatAutoStartRule"/> only auto-enters the fight once engaged, so re-landing on
        /// a platform whose battle already began resumes it.
        /// </summary>
        public bool Engaged { get; set; }

        /// <summary>
        /// Who caused this fight — decides who leads the opening round (D2). Defaults to
        /// <see cref="CombatInitiator.Enemy"/> (an ambush/aggro cross, or an NPC that turned hostile),
        /// flipped to <see cref="CombatInitiator.Player"/> only when the player chose to attack (the
        /// encounter's Attack card). Read by <c>CombatActiveState</c> when the fight begins.
        /// </summary>
        public CombatInitiator Initiator { get; set; } = CombatInitiator.Enemy;

        // Enemy lifecycle state
        private bool _hasBeenInstantiated;
        private IPlayer _enemyPlayer;
        private EnemyCombatComponent _enemyCombatComponent;

        /// <summary>
        /// Indicates whether enemy has been instantiated (first-time only).
        /// </summary>
        public bool HasBeenInstantiated => _hasBeenInstantiated;

        /// <summary>
        /// Indicates whether enemy is alive.
        /// Returns false if enemy has not been instantiated or if HP reached 0.
        /// </summary>
        public bool IsEnemyAlive => _enemyCombatComponent?.IsAlive ?? false;

        /// <summary>
        /// The AIPlayer that controls this enemy.
        /// </summary>
        public IPlayer EnemyPlayer => _enemyPlayer;

        /// <summary>
        /// The combat component managing this enemy's combat state.
        /// </summary>
        public EnemyCombatComponent EnemyCombatComponent => _enemyCombatComponent;

        public override void Initialize(IPlatform platform)
        {
            // Enemy content initialization - no longer type-checks platform
            // State factory handles appropriate state selection based on content
        }

        public override void OnPlatformEntered(IPlatform platform)
        {
            // Enemy instantiation happens in PlatformIdleState
            // Combat integration happens in CombatActiveState
        }

        /// <summary>
        /// Called by PlatformIdleState to instantiate enemy FIRST TIME ONLY.
        /// Stores enemy player and combat component references for later combat integration.
        /// </summary>
        public void InstantiateEnemy(IPlayer enemyPlayer, EnemyCombatComponent combatComponent)
        {
            if (_hasBeenInstantiated)
            {
                Logger?.Warning(LogCategory.Platform, $"[EnemyContent] Enemy {EnemyId} already instantiated!");
                return;
            }

            _enemyPlayer = enemyPlayer;
            _enemyCombatComponent = combatComponent;
            _hasBeenInstantiated = true;

            Logger?.Info(LogCategory.Platform, $"[EnemyContent] Enemy {EnemyId} instantiated with player {enemyPlayer.Name}");
        }
    }
}

