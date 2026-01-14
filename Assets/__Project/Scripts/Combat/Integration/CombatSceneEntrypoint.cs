using Combat.Controller;
using Combat.Core;
using Combat.Player;
using Combat.View;
using Combat.Integration;
using UnityEngine;
using System.Collections.Generic;
using Zenject;
using Combat.Battlefield;

namespace Combat.Integration
{
    /// <summary>
    /// Scene entrypoint for combat scenes.
    /// Initializes the combat system and connects all components.
    /// </summary>
    public class CombatSceneEntrypoint : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private CombatStateView _gameStateView;
        [SerializeField] private CombatUIController _uiController;
        [SerializeField] private CombatBattlefieldView _battlefieldView;
        [SerializeField] private HumanPlayerController _humanPlayerController;
        
        [Header("Configuration")]
        [SerializeField] private bool _autoStart = true;
        [SerializeField] private bool _includeAI = true;
        
        [Inject] private ICombatController _gameController;
        [Inject] private CombatBattlefield _combatBattlefield;
        [Inject] private IBattlefield _battlefield;
        
        private IPlayer _humanPlayer;
        private IPlayer _aiPlayer;
        
        private void Start()
        {
            if (_autoStart)
            {
                InitializeCombat();
            }
        }
        
        /// <summary>
        /// Initializes the combat system with test data.
        /// </summary>
        public void InitializeCombat()
        {
            // Create players
            _humanPlayer = new HumanPlayer(1, "Player 1");
            
            List<IPlayer> players = new List<IPlayer> { _humanPlayer };
            
            if (_includeAI)
            {
                var aiDecisionMaker = new SimpleRandomAI();
                _aiPlayer = new AIPlayer(2, "AI", aiDecisionMaker);
                players.Add(_aiPlayer);
            }
            
            // Create test units
            var units = CreateTestUnits();
            
            // Create initial game state
            var initialState = new CombatState(
                units,
                players,
                _humanPlayer,
                1,
                CombatPhase.Setup,
                null  // Battlefield will be injected later if needed
            );
            
            // Initialize game controller
            _gameController.Initialize(initialState, players);
            
            // Initialize views
            _gameStateView.Initialize(_gameController, _humanPlayer.Id);
            _uiController.Initialize(_gameController);
            _battlefieldView.Initialize(_combatBattlefield, _gameController);
            _humanPlayerController.Initialize(_humanPlayer, _gameController);
            
            // Connect events
            _humanPlayerController.OnUnitSelected += _uiController.OnUnitSelected;
            _gameController.OnTurnStarted += OnTurnStarted;
            _gameController.OnGameEnded += OnGameEnded;
            
            Debug.Log("Combat initialized successfully!");
        }
        
        private List<IUnit> CreateTestUnits()
        {
            // Create abilities
            var meleeAttack = new MeleeAttackAbility(10);
            var powerAttack = new PowerAttackAbility(25);
            var heal = new HealAbility(15);
            var poisonStrike = new PoisonStrikeAbility(8, 5, 3);
            
            var abilities = new List<IAbilityInstance>
            {
                new AbilityInstance(meleeAttack),
                new AbilityInstance(powerAttack),
                new AbilityInstance(heal),
                new AbilityInstance(poisonStrike)
            };
            
            var units = new List<IUnit>();
            
            // Player units
            units.Add(new Unit(1, _humanPlayer, new HexCoordinates(0, 0), 50, 50, abilities));
            units.Add(new Unit(2, _humanPlayer, new HexCoordinates(1, 0), 50, 50, abilities));
            
            // AI units (if enabled)
            if (_includeAI && _aiPlayer != null)
            {
                units.Add(new Unit(3, _aiPlayer, new HexCoordinates(0, 5), 50, 50, abilities));
                units.Add(new Unit(4, _aiPlayer, new HexCoordinates(1, 5), 50, 50, abilities));
            }
            
            return units;
        }
        
        private void OnTurnStarted(IPlayer player)
        {
            Debug.Log($"{player.Name}'s turn started!");
            
            if (player.Type == PlayerType.AI)
            {
                StartCoroutine(ProcessAITurn(player));
            }
        }
        
        private void OnGameEnded(IPlayer winner, CombatPhase phase)
        {
            if (winner != null)
            {
                Debug.Log($"Game Over! {winner.Name} wins!");
            }
            else
            {
                Debug.Log("Game Over!");
            }
        }
        
        private System.Collections.IEnumerator ProcessAITurn(IPlayer aiPlayer)
        {
            var ai = aiPlayer as AIPlayer;
            var activeUnits = _gameController.CombatState.GetActiveUnitsByPlayer(aiPlayer);
            
            foreach (var unit in activeUnits)
            {
                yield return new WaitForSeconds(0.5f);
                
                var action = ai.RequestAction(_gameController.CombatState, unit);
                _gameController.ProcessAction(action);
            }
        }
    }
}

