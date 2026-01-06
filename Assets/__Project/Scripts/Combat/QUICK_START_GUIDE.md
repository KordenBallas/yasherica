# Combat System Quick Start Guide

## Overview

This guide shows how to set up and use the turn-based combat system in your Unity project.

---

## 1. Basic Setup

### Step 1: Create Combat Scene

1. Create a new scene: `Scenes/CombatTest.unity`
2. Add a `Plane` for the ground
3. Add a `Camera` positioned above the battlefield
4. Add an empty GameObject named `CombatManager`

### Step 2: Set Up Dependencies

The combat system requires these dependencies (inject via Zenject):

```csharp
// In your installer (e.g., CombatSceneInstaller.cs)
using Combat.Core;
using Combat.TurnManagement;
using Combat.Execution;
using Combat.Controller;
using Zenject;

public class CombatSceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Core systems
        Container.Bind<IDamageSystem>().To<DamageSystem>().AsSingle();
        Container.Bind<IActionValidator>().To<ActionValidator>().AsSingle();
        Container.Bind<IAbilityExecutor>().To<AbilityExecutor>().AsSingle();
        Container.Bind<IActionExecutor>().To<ActionExecutor>().AsSingle();
        Container.Bind<ITurnManager>().To<TurnManager>().AsSingle();
        Container.Bind<IGameController>().To<GameController>().AsSingle();
    }
}
```

---

## 2. Initialize a Game

### Create Players

```csharp
using Combat.Core;
using Combat.Player;

// Human player
var humanPlayer = new HumanPlayer(id: 1, name: "Player 1");

// AI player
var aiDecisionMaker = new SimpleRandomAI();
var aiPlayer = new AIPlayer(id: 2, name: "AI", aiDecisionMaker);

var players = new List<IPlayer> { humanPlayer, aiPlayer };
```

### Create Units

```csharp
using Combat.Core;
using Combat.Battlefield;

// Create abilities
var meleeAttack = new MeleeAttackAbility(damage: 10);
var powerAttack = new PowerAttackAbility(damage: 25);

var abilities = new List<IAbilityInstance>
{
    new AbilityInstance(meleeAttack),
    new AbilityInstance(powerAttack)
};

// Create unit
var unit1 = new Unit(
    id: 1,
    owner: humanPlayer,
    position: new HexCoordinates(0, 0),
    currentHP: 50,
    maxHP: 50,
    abilities: abilities
);

// Create more units...
var units = new List<IUnit> { unit1, unit2, unit3, unit4 };
```

### Create Initial Game State

```csharp
using Combat.Core;

var initialState = new GameState(
    units: units,
    players: players,
    currentPlayer: players[0],
    turnNumber: 1,
    phase: GamePhase.Setup
);
```

### Initialize Game Controller

```csharp
using Combat.Controller;

// Get from Zenject container
IGameController gameController = Container.Resolve<IGameController>();

// Initialize
gameController.Initialize(initialState, players);
```

---

## 3. Set Up Views

### Create Unit View Prefab

1. Create a new GameObject in the scene
2. Add a `Cube` as visual representation
3. Add a 3D `TextMeshPro` for HP display (position above the cube)
4. Add `UnitView` component
5. Assign references in inspector
6. Save as prefab: `Prefabs/UnitViewPrefab.prefab`

### Add GameStateView

```csharp
using Combat.View;
using UnityEngine;

public class CombatSceneController : MonoBehaviour
{
    [SerializeField] private GameStateView _gameStateView;
    [SerializeField] private GameObject _unitViewPrefab;
    
    private IGameController _gameController;
    
    void Start()
    {
        // Get game controller from Zenject
        _gameController = GetComponent<IGameController>();
        
        // Initialize view
        _gameStateView.Initialize(_gameController, localPlayerId: 1);
    }
}
```

### Add UI

1. Create a Canvas in the scene
2. Add TextMeshPro elements for:
   - Turn number
   - Current player
   - Selected unit info
   - Game status
3. Add `CombatUIController` component
4. Assign references
5. Initialize in code:

```csharp
[SerializeField] private CombatUIController _uiController;

void Start()
{
    _uiController.Initialize(_gameController);
}
```

---

## 4. Set Up Human Player Input

```csharp
using Combat.Player;
using Combat.View;

public class CombatSceneController : MonoBehaviour
{
    [SerializeField] private HumanPlayerController _humanPlayerController;
    [SerializeField] private CombatUIController _uiController;
    [SerializeField] private ActionPreviewView _actionPreview;
    
    void Start()
    {
        // ... previous initialization ...
        
        // Set up human player controller
        _humanPlayerController.Initialize(humanPlayer, _gameController);
        
        // Connect events
        _humanPlayerController.OnUnitSelected += OnUnitSelected;
        _humanPlayerController.OnActionRequested += OnActionRequested;
    }
    
    void OnUnitSelected(IUnit unit)
    {
        _uiController.OnUnitSelected(unit);
        
        // Show valid move positions
        var validPositions = _humanPlayerController.GetValidMovePositions();
        _actionPreview.ShowMovementRange(validPositions);
    }
    
    void OnActionRequested(IAction action)
    {
        Debug.Log($"Action requested: {action.Type}");
    }
}
```

---

## 5. Handle AI Turns

```csharp
using Combat.Core;
using Combat.Player;

public class CombatSceneController : MonoBehaviour
{
    void Start()
    {
        // ... previous initialization ...
        
        // Subscribe to turn events
        _gameController.OnTurnStarted += OnTurnStarted;
    }
    
    void OnTurnStarted(IPlayer player)
    {
        if (player.Type == PlayerType.AI)
        {
            StartCoroutine(ProcessAITurn(player));
        }
    }
    
    IEnumerator ProcessAITurn(IPlayer aiPlayer)
    {
        // Get AI player
        var ai = aiPlayer as AIPlayer;
        
        // Get active units
        var activeUnits = _gameController.GameState.GetActiveUnitsByPlayer(aiPlayer);
        
        foreach (var unit in activeUnits)
        {
            // Wait a bit for visual feedback
            yield return new WaitForSeconds(0.5f);
            
            // AI decides action
            var action = ai.RequestAction(_gameController.GameState, unit);
            
            // Execute action
            _gameController.ProcessAction(action);
        }
    }
}
```

---

## 6. Example: Full Scene Setup

```csharp
using Combat.Core;
using Combat.Player;
using Combat.Controller;
using Combat.View;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Combat.Battlefield;

public class CombatSceneController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameStateView _gameStateView;
    [SerializeField] private HumanPlayerController _humanPlayerController;
    [SerializeField] private CombatUIController _uiController;
    [SerializeField] private ActionPreviewView _actionPreview;
    [SerializeField] private GameObject _unitViewPrefab;
    
    private IGameController _gameController;
    private IPlayer _humanPlayer;
    private IPlayer _aiPlayer;
    
    void Start()
    {
        SetupGame();
    }
    
    void SetupGame()
    {
        // Create players
        _humanPlayer = new HumanPlayer(1, "Player");
        var aiDecisionMaker = new SimpleRandomAI();
        _aiPlayer = new AIPlayer(2, "AI", aiDecisionMaker);
        
        // Create units
        var units = CreateTestUnits();
        
        // Create initial state
        var initialState = new GameState(
            units,
            new List<IPlayer> { _humanPlayer, _aiPlayer },
            _humanPlayer,
            1,
            GamePhase.Setup
        );
        
        // Get controller (from Zenject or create manually)
        _gameController = CreateGameController();
        
        // Initialize
        _gameController.Initialize(initialState, new List<IPlayer> { _humanPlayer, _aiPlayer });
        
        // Set up views
        _gameStateView.Initialize(_gameController, _humanPlayer.Id);
        _uiController.Initialize(_gameController);
        _humanPlayerController.Initialize(_humanPlayer, _gameController);
        
        // Connect events
        _humanPlayerController.OnUnitSelected += _uiController.OnUnitSelected;
        _humanPlayerController.OnUnitSelected += OnUnitSelected;
        _gameController.OnTurnStarted += OnTurnStarted;
        _gameController.OnGameEnded += OnGameEnded;
    }
    
    List<IUnit> CreateTestUnits()
    {
        var meleeAttack = new MeleeAttackAbility(10);
        var powerAttack = new PowerAttackAbility(25);
        var heal = new HealAbility(15);
        
        var abilities = new List<IAbilityInstance>
        {
            new AbilityInstance(meleeAttack),
            new AbilityInstance(powerAttack),
            new AbilityInstance(heal)
        };
        
        return new List<IUnit>
        {
            new Unit(1, _humanPlayer, new HexCoordinates(0, 0), 50, 50, abilities),
            new Unit(2, _humanPlayer, new HexCoordinates(1, 0), 50, 50, abilities),
            new Unit(3, _aiPlayer, new HexCoordinates(0, 3), 50, 50, abilities),
            new Unit(4, _aiPlayer, new HexCoordinates(1, 3), 50, 50, abilities)
        };
    }
    
    IGameController CreateGameController()
    {
        var damageSystem = new DamageSystem();
        var abilityExecutor = new AbilityExecutor(damageSystem);
        var actionExecutor = new ActionExecutor(abilityExecutor);
        var actionValidator = new ActionValidator();
        var turnManager = new TurnManager();
        
        return new GameController(actionValidator, actionExecutor, turnManager, damageSystem);
    }
    
    void OnUnitSelected(IUnit unit)
    {
        var validPositions = _humanPlayerController.GetValidMovePositions();
        _actionPreview.ShowMovementRange(validPositions);
    }
    
    void OnTurnStarted(IPlayer player)
    {
        Debug.Log($"{player.Name}'s turn started!");
        
        if (player.Type == PlayerType.AI)
        {
            StartCoroutine(ProcessAITurn(player));
        }
    }
    
    void OnGameEnded(IPlayer winner, GamePhase phase)
    {
        if (winner != null)
        {
            Debug.Log($"Game Over! {winner.Name} wins!");
        }
    }
    
    IEnumerator ProcessAITurn(IPlayer aiPlayer)
    {
        var ai = aiPlayer as AIPlayer;
        var activeUnits = _gameController.GameState.GetActiveUnitsByPlayer(aiPlayer);
        
        foreach (var unit in activeUnits)
        {
            yield return new WaitForSeconds(0.5f);
            var action = ai.RequestAction(_gameController.GameState, unit);
            _gameController.ProcessAction(action);
        }
    }
}
```

---

## 7. Player Actions

### Move Unit

```csharp
// From HumanPlayerController or custom code
_humanPlayerController.RequestMoveAction(targetPosition);
```

### Use Ability

```csharp
// Schedule ability
var target = AbilityTarget.ForUnit(enemyUnitId, AbilityTargetType.Enemy);
_humanPlayerController.RequestScheduleAbility(abilityId, target);

// Execute queue
_humanPlayerController.RequestExecuteAbilityQueue();
```

### End Turn

```csharp
_humanPlayerController.RequestEndUnitTurn();
```

---

## 8. Extending the System

### Add New Ability

```csharp
public class FireballAbility : Ability, IDamageAbility
{
    public int Damage { get; }
    
    public FireballAbility(int damage = 30)
        : base(5, "Fireball", 3, AbilityTargetType.Enemy, 3, AbilityEffectType.Damage)
    {
        Damage = damage;
    }
}
```

### Add New Win Condition

```csharp
public class CapturePointWinCondition : IWinCondition
{
    public WinConditionType Type => WinConditionType.ReachObjective;
    
    private HexCoordinates _capturePoint;
    
    public CapturePointWinCondition(HexCoordinates capturePoint)
    {
        _capturePoint = capturePoint;
    }
    
    public bool Check(IGameState gameState, out IPlayer winningPlayer)
    {
        var unitAtPoint = gameState.GetUnitAt(_capturePoint);
        if (unitAtPoint != null && unitAtPoint.IsAlive)
        {
            winningPlayer = unitAtPoint.Owner;
            return true;
        }
        
        winningPlayer = null;
        return false;
    }
}
```

### Add New Status Effect

```csharp
public class BurningEffect : StatusEffect
{
    public int DamagePerTurn { get; }
    
    public BurningEffect(int damagePerTurn = 7, int duration = 2)
        : base(1004, "Burning", StatusEffectType.DamageOverTime, duration, 1, true)
    {
        DamagePerTurn = damagePerTurn;
    }
}
```

---

## Tips & Best Practices

1. **Always validate** - Use `ActionValidator` before executing actions
2. **Immutability** - Never modify GameState directly, always use `With*()` methods
3. **Events** - Subscribe to `OnStateChanged`, `OnTurnStarted`, `OnGameEnded` for UI updates
4. **Testing** - Create unit tests for Core layer (no Unity dependencies)
5. **Performance** - Combat is turn-based, no need to optimize for real-time
6. **Extensibility** - Use interfaces (`IAbility`, `IWinCondition`, `IPlayer`) for new features

---

## Troubleshooting

**Q: Units not moving?**
- Check `ActionValidator` logs for validation errors
- Verify target position is not occupied
- Ensure unit `CanAct` is true

**Q: Abilities not executing?**
- Check if ability is on cooldown (`IsAvailable`)
- Verify target is valid (range, type)
- Ensure queue is not empty when executing

**Q: AI not taking turns?**
- Verify `OnTurnStarted` event is connected
- Check if AI player type is set correctly
- Ensure coroutine is started for AI processing

**Q: UI not updating?**
- Verify `OnStateChanged` subscription
- Check if `CombatUIController` is initialized
- Ensure TextMeshPro references are assigned

---

**Ready to play!** 🎮

