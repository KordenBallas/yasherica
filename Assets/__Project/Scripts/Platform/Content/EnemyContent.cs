using Combat.Enemy;
using Combat.Core;
using UnityEngine;

namespace Platform
{
    public class EnemyContent : PlatformContentBase
    {
        public override ContentType Type => ContentType.Enemy;

        public int EnemyId { get; set; }

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
            // Validate that enemy content is on a combat platform
            if (platform is not CombatPlatform)
            {
                Debug.LogWarning($"[EnemyContent] Enemy content should be placed on CombatPlatform, but found on {platform.GetType().Name}");
            }
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
                Debug.LogWarning($"[EnemyContent] Enemy {EnemyId} already instantiated!");
                return;
            }

            _enemyPlayer = enemyPlayer;
            _enemyCombatComponent = combatComponent;
            _hasBeenInstantiated = true;

            Debug.Log($"[EnemyContent] Enemy {EnemyId} instantiated with player {enemyPlayer.Name}");
        }
    }
}

