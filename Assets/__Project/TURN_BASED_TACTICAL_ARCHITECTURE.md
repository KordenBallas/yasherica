# Turn-Based Tactical Game Architecture
## Turn-Based Tactical Combat Architecture

---

## STAGE 1. ARCHITECTURAL ANALYSIS

### 1.1 High-Level Architecture (Layers)

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                        │
│  (Unity MonoBehaviour, UI, Rendering, Input)                │
│  - GameView, UnitView, BattlefieldView, UIManager           │
│  - Depends on: Core, Combat                                  │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │
┌─────────────────────────────────────────────────────────────┐
│                    NETWORKING LAYER                          │
│  (Client/Server Communication, Serialization)               │
│  - NetworkClient, NetworkServer, ActionSerializer          │
│  - Depends on: Core, Combat                                  │
│  - Does NOT depend on: Unity (pure C#)                       │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │
┌─────────────────────────────────────────────────────────────┐
│                    COMBAT LAYER                              │
│  (Turn Management, Actions, Validation)                      │
│  - TurnManager, ActionExecutor, ActionValidator            │
│  - Depends on: Core                                          │
│  - Does NOT depend on: Unity, Networking                     │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │
┌─────────────────────────────────────────────────────────────┐
│                    CORE LAYER                                │
│  (Game State, Units, Rules, Win Conditions)                 │
│  - GameState, Unit, Action, WinCondition                   │
│  - Does NOT depend on: Unity, Networking, Combat             │
│  - Single Source of Truth                                    │
└─────────────────────────────────────────────────────────────┘
                              ▲
                              │
┌─────────────────────────────────────────────────────────────┐
│                    PLAYER LAYER                              │
│  (Player Abstractions: Human, AI, Network)                  │
│  - IPlayer, HumanPlayer, AIPlayer, NetworkPlayer           │
│  - Depends on: Core, Combat                                  │
│  - Does NOT depend on: Unity (except HumanPlayer)            │
└─────────────────────────────────────────────────────────────┘
```

**Responsibility Boundaries:**

1. **Core Layer** (pure logic):
   - Stores game state (GameState)
   - Defines rules (Rules)
   - Contains game entities (Unit, Action)
   - Does NOT know about Unity, networking, visualization
   - Can be tested with unit tests

2. **Combat Layer** (battle management):
   - Manages turn order (TurnManager)
   - Executes actions (ActionExecutor)
   - Validates actions (ActionValidator)
   - Depends only on Core
   - Does NOT know about Unity, networking

3. **Networking Layer** (network interaction):
   - Serializes/deserializes Actions
   - Sends/receives messages
   - Synchronizes GameState between clients
   - Depends on Core and Combat
   - Does NOT depend on Unity

4. **Player Layer** (players):
   - Player abstraction (IPlayer)
   - Implementations: HumanPlayer (Unity input), AIPlayer (logic), NetworkPlayer (network)
   - Depends on Core and Combat
   - HumanPlayer depends on Unity, others don't

5. **Presentation Layer** (visualization):
   - Unity MonoBehaviour for display
   - UI, animations, effects
   - Reads GameState, does NOT modify directly
   - All changes through Actions

---

### 1.2 Key Entities and Their Roles

#### **GameState** (Core Layer)
**Responsibility:**
- Single source of truth about game state
- Stores all units, their positions, statuses
- Stores current active player
- **IMMUTABLE** - every change creates new state

**Should NOT:**
- Know about Unity
- Know about networking
- Manage turn order (that's TurnManager)
- Execute actions (that's ActionExecutor)
- Validate actions (that's ActionValidator)

**Interface (conceptual):**
```
IGameState
  - IReadOnlyList<IUnit> Units { get; }
  - IReadOnlyList<IPlayer> Players { get; }
  - IPlayer CurrentPlayer { get; }
  - int TurnNumber { get; }
  - GamePhase Phase { get; } // Setup, Combat, Victory, Defeat
  
  // Query methods:
  - IUnit GetUnit(int unitId);
  - IUnit GetUnitAt(HexCoordinates position);
  - IReadOnlyList<IUnit> GetUnitsByPlayer(IPlayer player);
  - IReadOnlyList<IUnit> GetActiveUnitsByPlayer(IPlayer player); // Units that can still act
```

**Note:** `GameState` - immutable value object. All changes create new instance.

---

#### **TurnManager** (Combat Layer)
**Responsibility:**
- Manages turn order
- Determines whose turn it is
- Switches turns
- Uses simple round-robin order

**Should NOT:**
- Store game state (that's GameState)
- Execute actions (that's ActionExecutor)
- Know about Unity or networking

**Interface (conceptual):**
```
ITurnManager
  - IPlayer CurrentPlayer { get; }
  - int CurrentTurnNumber { get; }
  - IReadOnlyList<IPlayer> TurnOrder { get; }
  
  - void Initialize(IReadOnlyList<IPlayer> players);
  - void NextTurn();
  - bool IsPlayerTurn(IPlayer player);
```

---

#### **IHasHealth** (Core Layer)
**Responsibility:**
- Represents entity with HP (health)
- Can be implemented by any game objects that have health

**Interface (conceptual):**
```
IHasHealth
  - int CurrentHP { get; }
  - int MaxHP { get; }
  - bool IsAlive { get; } // CurrentHP > 0
```

---

#### **IAbility** (Core Layer)
**Responsibility:**
- Represents ability that unit can use
- Contains base ability parameters
- Defines effect type and targets

**Interface (conceptual):**
```
IAbility
  - int Id { get; }
  - string Name { get; }
  - int CooldownDuration { get; } // Turns before can be used again
  - AbilityTargetType TargetType { get; } // Enemy, Ally, Self, Position, Area
  - int Range { get; } // Application range (in hex cells)
  - AbilityEffectType EffectType { get; } // Damage, Heal, StatusEffect, Hybrid
```

**Note:**
- Ability doesn't execute itself - that's `IAbilityExecutor`'s job
- Specific effects (damage, healing) defined by subtypes (see IDamageAbility)

---

#### **Ability Effect Subtypes**

```csharp
// For damage abilities:
public interface IDamageAbility : IAbility
{
    int Damage { get; }
}

// For healing abilities:
public interface IHealAbility : IAbility
{
    int HealAmount { get; }
}

// For status effect abilities:
public interface IStatusEffectAbility : IAbility
{
    IStatusEffect EffectToApply { get; }
    int EffectDuration { get; }
}

// Combined abilities can implement multiple interfaces
public class PoisonStrikeAbility : IDamageAbility, IStatusEffectAbility
{
    public int Damage { get; set; } = 10;
    public IStatusEffect EffectToApply { get; set; } = new PoisonEffect();
    public int EffectDuration { get; set; } = 3;
}
```

---

#### **IAbilityInstance** (Core Layer)
**Responsibility:**
- Represents specific ability instance on unit
- Tracks current cooldown state

**Interface (conceptual):**
```
IAbilityInstance
  - IAbility Ability { get; }
  - int CurrentCooldown { get; } // Remaining turns until available
  - bool IsAvailable { get; } // CurrentCooldown == 0
```

---

#### **ScheduledAbility** (Core Layer)
**Responsibility:**
- Represents ability in unit's queue awaiting execution
- Contains information about target and execution order

**Structure:**
```
ScheduledAbility (struct)
  - IAbilityInstance Ability { get; }
  - AbilityTarget Target { get; } // Ability target
  - int ExecutionOrder { get; } // Order in queue (0 = first)
```

**AbilityTarget (struct):**
```
AbilityTarget
  - AbilityTargetType Type { get; } // Unit, Position, Self
  - int? TargetUnitId { get; } // If target is unit
  - HexCoordinates? TargetPosition { get; } // If target is position
```

**Queue Rules:**
- Maximum 1 ability can be scheduled per turn for unit
- Abilities execute in ExecutionOrder
- Player can modify queue (reorder, retarget) while unit hasn't acted
- After queue execution, unit becomes inactive (HasActedThisTurn = true)

---

#### **IStatusEffect** (Core Layer)
**Responsibility:**
- Представляет временный эффект на юните
- Может изменять характеристики или наносить урон/лечение со временем

**Interface (conceptual):**
```
IStatusEffect
  - int Id { get; }
  - string Name { get; }
  - StatusEffectType Type { get; } // Buff, Debuff, DamageOverTime, HealOverTime
  - int Duration { get; } // Remaining turns (-1 = infinite)
  - int StackCount { get; } // Number of stacks (some effects stack)
  - bool IsStackable { get; }
```

**StatusEffectType (enum):**
```
public enum StatusEffectType
{
    Buff,           // Positive effect
    Debuff,         // Negative effect
    DamageOverTime, // Damage each turn (poison, bleeding)
    HealOverTime,   // Healing each turn (regeneration)
    Control         // Control (stun, immobilize)
}
```

**Примеры эффектов:**
```csharp
// Poison - deals damage each turn
public class PoisonEffect : IStatusEffect
{
    public int DamagePerTurn { get; set; } = 5;
    public int Duration { get; set; } = 3; // 3 turns
}

// Regeneration - heals each turn
public class RegenerationEffect : IStatusEffect
{
    public int HealPerTurn { get; set; } = 3;
    public int Duration { get; set; } = 5;
}

// Stun - unit can't act
public class StunEffect : IStatusEffect
{
    public int Duration { get; set; } = 1; // 1 turn
}
```

**Effect Application:**
- Effects are processed at start/end of unit's turn
- ActionExecutor is responsible for applying effects
- Cooldowns decrement at end of turn

---

#### **Unit** (Core Layer)
**Responsibility:**
- Представляет игровую единицу (персонаж, враг)
- Хранит характеристики: позиция, способности, статусы
- Отслеживает состояние в течение хода (действовал ли, запланированные способности)
- Принадлежит игроку (IPlayer)
- Реализует IHasHealth для управления здоровьем

**Should NOT:**
- Знать о Unity
- Знать о сети
- Управлять своим ходом (это TurnManager)
- Выполнять действия напрямую (через Actions)

**Interface (conceptual):**
```
IUnit : IHasHealth
  - int Id { get; }
  - IPlayer Owner { get; }
  - HexCoordinates Position { get; }
  - IReadOnlyList<IAbilityInstance> Abilities { get; }
  - IReadOnlyList<ScheduledAbility> AbilityQueue { get; } // Запланированные способности
  - IReadOnlyList<IStatusEffect> StatusEffects { get; }
  
  // State tracking (per turn):
  - bool HasActedThisTurn { get; } // Выполнил ли юнит основное действие
  - bool CanAct { get; } // Может ли действовать (не оглушен, жив, не действовал)
  - UnitActionState ActionState { get; } // Ready, ActedThisTurn, Stunned, Dead
  
  // Query methods:
  - bool CanMove();
  - IAbilityInstance GetAbility(int abilityId);
  - IReadOnlyList<IAbilityInstance> GetAvailableAbilities(); // With CurrentCooldown == 0
  - IReadOnlyList<HexCoordinates> GetValidMoveTargets(IBattlefield battlefield);
  - bool CanScheduleAbility(); // Checks if can still add to queue
```

**Note:** 
- Юнит может выполнить ОДНО основное действие за ход (ScheduleAbility или ExecuteAbilityQueue)
- После основного действия `HasActedThisTurn = true` и юнит не может больше действовать
- Пока юнит не действовал, можно изменять очередь способностей (reorder, retarget)

---

#### **Actions** (Core Layer)
**Responsibility:**
- Представляет намерение игрока (Command Pattern)
- Immutable структура данных
- Содержит все данные для выполнения действия
- **Конкретные классы вместо словарей для type safety**

**Should NOT:**
- Выполнять себя сам (это ActionExecutor)
- Знать о Unity или сети
- Валидировать себя (это ActionValidator)

**Базовый интерфейс:**
```
IAction
  - IPlayer Player { get; }
  - int UnitId { get; }
  - ActionType Type { get; }
  - bool EndsTurn { get; } // Заканчивает ли действие ход игрока
```

**Классификация Actions:**

### **Turn-Ending Actions** (заканчивают ход юнита):

**MoveAction** - перемещение юнита
```csharp
public class MoveAction : IAction
{
    public IPlayer Player { get; }
    public int UnitId { get; }
    public HexCoordinates TargetPosition { get; }
    public bool EndsTurn => true;
}
```

**ScheduleAbilityAction** - добавление способности в очередь
```csharp
public class ScheduleAbilityAction : IAction
{
    public IPlayer Player { get; }
    public int UnitId { get; }
    public int AbilityId { get; }
    public AbilityTarget Target { get; }
    public bool EndsTurn => false; // НЕ заканчивает ход, можно планировать дальше
}

// ВАЖНО: Максимум 1 способность может быть запланирована за ход
// После планирования способности можно:
// - Переупорядочить очередь (ReorderAbilitiesAction)
// - Изменить цель (RetargetAbilityAction)
// - Выполнить очередь (ExecuteAbilityQueueAction) - ЭТО заканчивает ход
```

**ExecuteAbilityQueueAction** - выполнение всех способностей в очереди
```csharp
public class ExecuteAbilityQueueAction : IAction
{
    public IPlayer Player { get; }
    public int UnitId { get; }
    public bool EndsTurn => true; // Заканчивает ход юнита
}

// После выполнения:
// - Все способности в очереди выполняются по порядку
// - Кулдауны запускаются
// - Юнит помечается как HasActedThisTurn = true
// - Очередь очищается
```

---

### **Non-Turn-Ending Actions** (НЕ заканчивают ход):

**ReorderAbilitiesAction** - изменение порядка способностей в очереди
```csharp
public class ReorderAbilitiesAction : IAction
{
    public IPlayer Player { get; }
    public int UnitId { get; }
    public IReadOnlyList<int> NewOrder { get; } // Индексы способностей в новом порядке
    public bool EndsTurn => false;
}
```

**RetargetAbilityAction** - изменение цели способности в очереди
```csharp
public class RetargetAbilityAction : IAction
{
    public IPlayer Player { get; }
    public int UnitId { get; }
    public int AbilityIndexInQueue { get; } // Индекс способности в очереди
    public AbilityTarget NewTarget { get; }
    public bool EndsTurn => false;
}
```

**EndUnitTurnAction** - явное завершение хода юнита без действий
```csharp
public class EndUnitTurnAction : IAction
{
    public IPlayer Player { get; }
    public int UnitId { get; }
    public bool EndsTurn => true;
}
```

---

#### **GameController** (Combat Layer)
**Responsibility:**
- Оркестрирует игровой цикл
- Координирует TurnManager, ActionExecutor, ActionValidator
- Управляет фазами игры (Setup, Combat, Victory)
- Проверяет win conditions

**Should NOT:**
- Хранить состояние (это GameState)
- Знать о Unity или сети
- Управлять очередностью напрямую (это TurnManager)

**Interface (conceptual):**
```
IGameController
  - IGameState GameState { get; }
  - ITurnManager TurnManager { get; }
  
  - void Initialize(IGameState initialState, IReadOnlyList<IPlayer> players);
  - ActionResult ProcessAction(IAction action);
  - void Update(); // Проверяет win conditions, переключает фазы
```

---

#### **PlayerController** (Player Layer)
**Responsibility:**
- Абстракция игрока (Human / AI / Network)
- Предоставляет намерения (Actions) в GameController
- Может запрашивать валидные действия для юнита

**Should NOT:**
- Выполнять действия (это ActionExecutor)
- Хранить состояние игры (это GameState)
- Знать о визуализации (кроме HumanPlayer)

**Interface (conceptual):**
```
IPlayer
  - int Id { get; }
  - PlayerType Type { get; } // Human, AI, Network
  - string Name { get; }
  
  - IAction RequestAction(IGameState gameState, IUnit unit);
  - IReadOnlyList<IAction> GetValidActions(IGameState gameState, IUnit unit);
```

**Реализации:**
- **HumanPlayer**: Ждет input от Unity, создает Action
- **AIPlayer**: Использует AI логику для выбора Action
- **NetworkPlayer**: Ждет Action от сети, валидирует и отправляет на сервер

---

### 1.3 Переход хода и применение Actions

**Модель хода:**
- У игрока есть несколько юнитов
- Игрок может взаимодействовать с каждым юнитом по очереди
- Каждый юнит может выполнить ОДНО главное действие за ход:
  - Переместиться (MoveAction)
  - Запланировать способность И выполнить очередь (ScheduleAbilityAction + ExecuteAbilityQueueAction)
  - Просто выполнить существующую очередь (ExecuteAbilityQueueAction)
- Пока юнит не выполнил главное действие, игрок может:
  - Изменять очередь способностей (ReorderAbilitiesAction)
  - Менять цели (RetargetAbilityAction)
  - Планировать новую способность (ScheduleAbilityAction) - максимум 1 за ход
- После главного действия юнит становится неактивным (HasActedThisTurn = true)
- Когда все юниты игрока действовали или игрок явно завершил ход (EndTurnAction), ход переходит к следующему игроку

**Поток выполнения:**

```
1. Начало хода игрока
   └─> TurnManager.CurrentPlayer устанавливается
   └─> Все юниты игрока: HasActedThisTurn = false
   └─> Декремент кулдаунов способностей
   └─> Применение StatusEffects (начало хода)

2. Игрок выбирает юнит для действия
   └─> Проверка: unit.CanAct && unit.Owner == CurrentPlayer
   
3. Игрок выполняет действие с юнитом
   └─> Варианты:
       a) ScheduleAbilityAction - добавляет способность в очередь (не заканчивает ход юнита)
       b) ReorderAbilitiesAction - меняет порядок в очереди (не заканчивает ход юнита)
       c) RetargetAbilityAction - меняет цель (не заканчивает ход юнита)
       d) ExecuteAbilityQueueAction - выполняет очередь (заканчивает ход юнита)
       e) MoveAction - перемещает юнит (заканчивает ход юнита)
       f) EndUnitTurnAction - пропускает ход юнита (заканчивает ход юнита)

4. Обработка действия:
   └─> ActionValidator.Validate(gameState, action)
       - Проверяет права игрока
       - Проверяет состояние юнита (CanAct)
       - Проверяет специфичные для действия условия
       
   └─> Если валидно:
       └─> ActionExecutor.Execute(gameState, action)
           └─> Создает новый GameState (immutable)
           └─> Если action.EndsTurn:
               └─> unit.HasActedThisTurn = true

5. Проверка завершения хода игрока:
   └─> Если все юниты игрока HasActedThisTurn == true:
       └─> Автоматический переход хода
   └─> Или игрок явно завершает ход (EndTurnAction)
   
6. Переход к следующему игроку:
   └─> TurnManager.NextTurn()
       └─> Применение StatusEffects (конец хода предыдущего игрока)
       └─> GameController проверяет win conditions
       └─> Возврат к шагу 1

7. Проверка условий победы:
   └─> После каждого действия GameController проверяет IWinCondition
   └─> Если условие выполнено → GamePhase = Victory/Defeat
```

**Кто имеет право применять Action:**
- Только текущий активный игрок (TurnManager.CurrentPlayer)
- ActionValidator проверяет, что action.Player == CurrentPlayer
- ActionValidator проверяет, что action.UnitId принадлежит CurrentPlayer
- ActionValidator проверяет, что юнит может действовать (CanAct == true)

**Где происходит валидация:**
- **ActionValidator** (Combat Layer):
  - Проверяет права игрока
  - Проверяет состояние юнита (HasActedThisTurn, StatusEffects)
  - Проверяет валидность цели (позиция, юнит существует, в радиусе)
  - Проверяет кулдауны способностей
  - Проверяет лимиты очереди (максимум 1 новая способность за ход)
  - Проверяет правила (дистанция, линия видимости)
  - НЕ зависит от Unity или сети

---

### 1.4 Updated Architecture Flow

**Detailed Turn Execution Flow:**

```
┌─────────────────────────────────────────────────────────────┐
│                     TURN START                               │
│  - Set CurrentPlayer (TurnManager)                           │
│  - Reset all units: HasActedThisTurn = false                 │
│  - Decrement ability cooldowns (CurrentCooldown--)           │
│  - Apply StatusEffects (OnTurnStart)                         │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              PLAYER SELECTS UNIT                             │
│  - Check: unit.CanAct && unit.Owner == CurrentPlayer         │
│  - Unit must have: HasActedThisTurn == false                 │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│           PLAYER PERFORMS ACTION WITH UNIT                   │
│                                                               │
│  Non-Turn-Ending Actions (can do multiple):                  │
│  ├─ ScheduleAbilityAction                                    │
│  │  └─ Add ability to AbilityQueue (max 1 NEW per turn)      │
│  ├─ ReorderAbilitiesAction                                   │
│  │  └─ Change execution order in queue                       │
│  └─ RetargetAbilityAction                                    │
│     └─ Change target of scheduled ability                    │
│                                                               │
│  Turn-Ending Actions (ends unit's turn):                     │
│  ├─ ExecuteAbilityQueueAction                                │
│  │  └─ Execute all abilities in queue → set cooldowns        │
│  ├─ MoveAction                                               │
│  │  └─ Move unit to new position                             │
│  └─ EndUnitTurnAction                                        │
│     └─ Skip unit's action                                    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                  ACTION VALIDATION                           │
│  - ActionValidator.Validate(gameState, action)               │
│  - Checks:                                                    │
│    • Player ownership (action.Player == CurrentPlayer)       │
│    • Unit ownership (unit.Owner == CurrentPlayer)            │
│    • Unit state (CanAct, not Stunned, not Dead)              │
│    • Action-specific rules (range, targets, cooldowns)       │
│    • Queue limits (max 1 new ability per turn)               │
└─────────────────────────────────────────────────────────────┘
                              ↓
                        [Valid?]
                         ↙    ↘
                    [Yes]      [No] → Return error to player
                      ↓
┌─────────────────────────────────────────────────────────────┐
│                  ACTION EXECUTION                            │
│  - ActionExecutor.Execute(gameState, action)                 │
│  - Returns NEW GameState (immutable)                         │
│  - If action.EndsTurn:                                       │
│    └─ unit.HasActedThisTurn = true                           │
│  - Special case for ExecuteAbilityQueueAction:               │
│    ├─ AbilityExecutor executes each ability in order         │
│    ├─ Apply damage/healing via DamageSystem                  │
│    ├─ Apply StatusEffects from abilities                     │
│    ├─ Set cooldowns (CurrentCooldown = CooldownDuration)     │
│    └─ Clear AbilityQueue                                     │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              CHECK TURN COMPLETION                           │
│  - Count active units: GetActiveUnitsByPlayer(CurrentPlayer) │
│  - If all units have HasActedThisTurn == true:               │
│    └─ Auto-advance to TURN END                               │
│  - Else:                                                      │
│    └─ Player can continue with other units                   │
│  - Player can manually end turn (EndTurnAction)              │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                     TURN END                                 │
│  - Apply StatusEffects (OnTurnEnd)                           │
│  - Process DOT/HOT effects (Poison, Regen)                   │
│  - Decrement StatusEffect durations                          │
│  - Remove expired effects                                    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              CHECK WIN CONDITIONS                            │
│  - GameController checks all registered IWinCondition        │
│  - If condition met:                                         │
│    └─ Set GamePhase = Victory/Defeat                         │
│    └─ End game                                               │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                  NEXT PLAYER                                 │
│  - TurnManager.NextTurn()                                    │
│  - Round-robin: next player in TurnOrder                     │
│  - Loop back to TURN START                                   │
└─────────────────────────────────────────────────────────────┘
```

**Key Points:**
- **Immutability**: Every action creates a new GameState
- **Per-Unit Actions**: Each unit acts independently within player's turn
- **Flexible Planning**: Can schedule/reorder abilities without ending turn
- **Explicit Execution**: Abilities execute only on player command
- **Cooldown Timing**: Cooldowns start after execution, not after scheduling
- **Auto Turn End**: Turn automatically ends when all units have acted
- **Win Check**: Checked after every action for immediate victory detection

---

## ЭТАП 2. ПЛАН СТРУКТУРЫ ПРОЕКТА

### 2.1 Структура папок

```
Scripts/
├── Core/                                    # Существующий
│   ├── DI/
│   ├── Events/
│   └── Utils/
│
├── Combat/                                  # НОВЫЙ: Turn-based combat
│   ├── Core/                                # Ядро боевой системы
│   │   ├── GameState.cs                     # Единый источник правды (IMMUTABLE)
│   │   ├── IGameState.cs
│   │   ├── IHasHealth.cs                    # Интерфейс для сущностей с HP
│   │   ├── Unit.cs                          # Игровая единица
│   │   ├── IUnit.cs
│   │   ├── UnitActionState.cs              # Enum: Ready, ActedThisTurn, Stunned, Dead
│   │   ├── Ability.cs                       # Способность
│   │   ├── IAbility.cs
│   │   ├── IDamageAbility.cs               # Способность с уроном
│   │   ├── IHealAbility.cs                 # Способность с лечением
│   │   ├── IStatusEffectAbility.cs         # Способность с эффектом
│   │   ├── AbilityInstance.cs              # Экземпляр способности на юните
│   │   ├── IAbilityInstance.cs
│   │   ├── ScheduledAbility.cs             # Способность в очереди юнита
│   │   ├── AbilityTarget.cs                # Цель способности (struct)
│   │   ├── AbilityTargetType.cs            # Enum: Enemy, Ally, Self, Position, Area
│   │   ├── AbilityEffectType.cs            # Enum: Damage, Heal, StatusEffect, Hybrid
│   │   ├── StatusEffect.cs                 # Эффект на юните
│   │   ├── IStatusEffect.cs
│   │   ├── StatusEffectType.cs             # Enum: Buff, Debuff, DOT, HOT, Control
│   │   ├── Action.cs                        # Базовый класс Action
│   │   ├── IAction.cs
│   │   ├── ActionType.cs                    # Enum: Move, ScheduleAbility, ExecuteQueue, Reorder, Retarget, EndTurn
│   │   ├── GamePhase.cs                     # Enum: Setup, Combat, Victory, Defeat
│   │   └── WinCondition.cs                  # Базовый класс win conditions
│   │   ├── IWinCondition.cs
│   │
│   ├── TurnManagement/                      # Управление ходами
│   │   ├── TurnManager.cs
│   │   └── ITurnManager.cs
│   │
│   ├── Actions/                             # Действия (конкретные классы)
│   │   ├── Base/
│   │   │   └── ActionBase.cs
│   │   ├── TurnEnding/                      # Действия, заканчивающие ход юнита
│   │   │   ├── MoveAction.cs
│   │   │   ├── ExecuteAbilityQueueAction.cs
│   │   │   └── EndUnitTurnAction.cs
│   │   ├── NonTurnEnding/                   # Действия, НЕ заканчивающие ход
│   │   │   ├── ScheduleAbilityAction.cs
│   │   │   ├── ReorderAbilitiesAction.cs
│   │   │   └── RetargetAbilityAction.cs
│   │   └── ActionFactory.cs                # Создание Actions из данных
│   │
│   ├── Execution/                           # Выполнение действий
│   │   ├── ActionExecutor.cs
│   │   ├── IActionExecutor.cs
│   │   ├── ActionValidator.cs
│   │   ├── IActionValidator.cs
│   │   ├── AbilityExecutor.cs              # Выполнение способностей
│   │   ├── IAbilityExecutor.cs
│   │   ├── DamageSystem.cs                 # Система урона
│   │   ├── IDamageSystem.cs
│   │   ├── ValidationResult.cs             # Детальный результат валидации
│   │   └── ActionResult.cs                  # Результат выполнения
│   │
│   ├── Rules/                               # Правила игры
│   │   ├── MovementRules.cs
│   │   ├── AttackRules.cs
│   │   ├── AbilityRules.cs
│   │   └── IRules.cs
│   │
│   ├── Controller/                          # Контроллер игры
│   │   ├── GameController.cs
│   │   ├── IGameController.cs
│   │   └── GameControllerConfig.cs
│   │
│   ├── Player/                              # Игроки
│   │   ├── Interfaces/
│   │   │   ├── IPlayer.cs
│   │   │   └── IPlayerController.cs
│   │   │
│   │   ├── Human/                           # Человеческий игрок
│   │   │   ├── HumanPlayer.cs
│   │   │   ├── HumanPlayerController.cs     # MonoBehaviour для input
│   │   │   └── ActionInputHandler.cs
│   │   │
│   │   ├── AI/                              # AI игрок
│   │   │   ├── AIPlayer.cs
│   │   │   ├── IAIDecisionMaker.cs
│   │   │   ├── SimpleAIDecisionMaker.cs
│   │   │   └── AIConfig.cs
│   │   │
│   │   └── Network/                         # Сетевой игрок
│   │       ├── NetworkPlayer.cs
│   │       ├── NetworkPlayerController.cs
│   │       └── NetworkActionReceiver.cs
│   │
│   ├── Networking/                          # Сетевое взаимодействие
│   │   ├── Core/
│   │   │   ├── INetworkClient.cs
│   │   │   ├── INetworkServer.cs
│   │   │   ├── NetworkClient.cs
│   │   │   ├── NetworkServer.cs
│   │   │   └── NetworkMessage.cs
│   │   │
│   │   ├── Serialization/
│   │   │   ├── ActionSerializer.cs
│   │   │   ├── GameStateSerializer.cs
│   │   │   └── IGameStateSerializer.cs
│   │   │
│   │   ├── Protocol/
│   │   │   ├── MessageType.cs               # Enum: Action, StateSync, Ping
│   │   │   ├── ClientMessage.cs
│   │   │   └── ServerMessage.cs
│   │   │
│   │   └── View/                            # Unity networking (Netcode)
│   │       ├── NetworkManager.cs            # MonoBehaviour (Netcode NetworkManager)
│   │       └── NetworkGameView.cs
│   │
│   ├── Battlefield/                         # Battlefield для Combat (расширение существующего)
│   │   ├── CombatBattlefield.cs             # Обертка над существующим IBattlefield
│   │   └── CombatBattlefieldView.cs        # View для Combat
│   │
│   └── View/                                # Unity представление
│       ├── GameStateView.cs                 # MonoBehaviour для отображения
│       ├── UnitView.cs
│       └── ActionPreviewView.cs
│
├── Battlefield/                             # Существующий (используется как есть)
│   ├── Core/
│   │   └── Battlefield.cs                   # Используется для позиционирования
│   └── ...
│
├── Platform/                                # Существующий
│   └── ...
│
└── Character/                               # Существующий (адаптировать)
    └── ...
```

---

### 2.2 Ключевые классы и интерфейсы

#### **Core Layer (Combat/Core/)**

**IHasHealth**
- `int CurrentHP { get; }`
- `int MaxHP { get; }`
- `bool IsAlive { get; }`

**IAbility**
- `int Id { get; }`
- `string Name { get; }`
- `int CooldownDuration { get; }`
- `AbilityTargetType TargetType { get; }`
- `int Range { get; }`
- `AbilityEffectType EffectType { get; }`

**IAbilityInstance**
- `IAbility Ability { get; }`
- `int CurrentCooldown { get; }`
- `bool IsAvailable { get; }`

**ScheduledAbility (struct)**
- `IAbilityInstance Ability { get; }`
- `AbilityTarget Target { get; }`
- `int ExecutionOrder { get; }`

**IStatusEffect**
- `int Id { get; }`
- `string Name { get; }`
- `StatusEffectType Type { get; }`
- `int Duration { get; }`
- `int StackCount { get; }`
- `bool IsStackable { get; }`

**IGameState (IMMUTABLE)**
- `IReadOnlyList<IUnit> Units { get; }`
- `IReadOnlyList<IPlayer> Players { get; }`
- `IPlayer CurrentPlayer { get; }`
- `int TurnNumber { get; }`
- `GamePhase Phase { get; }`
- `IUnit GetUnit(int unitId)`
- `IUnit GetUnitAt(HexCoordinates position)`
- `IReadOnlyList<IUnit> GetUnitsByPlayer(IPlayer player)`
- `IReadOnlyList<IUnit> GetActiveUnitsByPlayer(IPlayer player)`

**IUnit : IHasHealth**
- `int Id { get; }`
- `IPlayer Owner { get; }`
- `HexCoordinates Position { get; }`
- `IReadOnlyList<IAbilityInstance> Abilities { get; }`
- `IReadOnlyList<ScheduledAbility> AbilityQueue { get; }`
- `IReadOnlyList<IStatusEffect> StatusEffects { get; }`
- `bool HasActedThisTurn { get; }`
- `bool CanAct { get; }`
- `UnitActionState ActionState { get; }`
- `bool CanMove()`
- `IAbilityInstance GetAbility(int abilityId)`
- `IReadOnlyList<IAbilityInstance> GetAvailableAbilities()`
- `bool CanScheduleAbility()`

**IAction (базовый интерфейс)**
- `IPlayer Player { get; }`
- `int UnitId { get; }`
- `ActionType Type { get; }`
- `bool EndsTurn { get; }`

**Конкретные Actions:**
- `MoveAction(HexCoordinates TargetPosition)` - EndsTurn = true
- `ScheduleAbilityAction(int AbilityId, AbilityTarget Target)` - EndsTurn = false
- `ExecuteAbilityQueueAction()` - EndsTurn = true
- `ReorderAbilitiesAction(IReadOnlyList<int> NewOrder)` - EndsTurn = false
- `RetargetAbilityAction(int AbilityIndexInQueue, AbilityTarget NewTarget)` - EndsTurn = false
- `EndUnitTurnAction()` - EndsTurn = true

**IWinCondition**
- `bool Check(IGameState gameState)`
- `WinConditionType Type { get; }`

**Приоритетные Win Conditions:**
- `EliminateAllEnemiesWinCondition` - уничтожить всех врагов

---

#### **Combat Layer (Combat/TurnManagement/, Combat/Execution/)**

**ITurnManager**
- `IPlayer CurrentPlayer { get; }`
- `int CurrentTurnNumber { get; }`
- `IReadOnlyList<IPlayer> TurnOrder { get; }`
- `void Initialize(IReadOnlyList<IPlayer> players)`
- `void NextTurn()`
- `bool IsPlayerTurn(IPlayer player)`

**IActionExecutor**
- `IGameState Execute(IGameState gameState, IAction action)` - Возвращает новый GameState
- `ActionResult ExecuteWithResult(IGameState gameState, IAction action)`

**IActionValidator**
- `bool Validate(IGameState gameState, IAction action)`
- `ValidationResult ValidateDetailed(IGameState gameState, IAction action)`

**IAbilityExecutor**
- `IGameState ExecuteAbility(IGameState gameState, IUnit caster, ScheduledAbility scheduledAbility)`
- `void ApplyAbilityEffects(IUnit caster, IUnit target, IAbility ability)`

**IDamageSystem**
- `IGameState ApplyDamage(IGameState gameState, IUnit target, int amount)`
- `IGameState ApplyHealing(IGameState gameState, IUnit target, int amount)`
- `int CalculateFinalDamage(IUnit attacker, IUnit target, int baseDamage)` // Для будущего расширения

**IGameController**
- `IGameState GameState { get; }`
- `ITurnManager TurnManager { get; }`
- `void Initialize(IGameState initialState, IReadOnlyList<IPlayer> players)`
- `ActionResult ProcessAction(IAction action)`
- `void Update()`

---

#### **Player Layer (Player/)**

**IPlayer**
- `int Id { get; }`
- `PlayerType Type { get; }`
- `string Name { get; }`
- `IAction RequestAction(IGameState gameState, IUnit unit)`
- `IReadOnlyList<IAction> GetValidActions(IGameState gameState, IUnit unit)`

**IPlayerController** (для Unity)
- `void Initialize(IPlayer player, IGameController gameController)`
- `void OnActionRequested(IAction action)`

---

#### **Networking Layer (Networking/)**

**INetworkClient**
- `void Connect(string serverAddress, int port)`
- `void SendAction(IAction action)`
- `void Disconnect()`
- `event Action<IGameState> OnStateReceived`
- `event Action<IAction> OnActionReceived`

**INetworkServer**
- `void Start(int port)`
- `void Stop()`
- `void BroadcastState(IGameState gameState)`
- `void ProcessAction(IAction action, int clientId)`
- `event Action<IAction, int> OnActionReceived`

---

### 2.3 Зависимости между компонентами

```
Presentation Layer
  └─> зависит от: Combat, Core, Player (Human)
  
Networking Layer
  └─> зависит от: Combat, Core, Player (Network)
  
Combat Layer
  └─> зависит от: Core
  └─> НЕ зависит от: Unity, Networking
  
Player Layer
  └─> зависит от: Core, Combat
  └─> HumanPlayer зависит от Unity
  └─> AIPlayer и NetworkPlayer - pure C#
  
Core Layer
  └─> НЕ зависит ни от чего (кроме стандартных библиотек)
  └─> Может использовать Battlefield для позиционирования
```

**Правило зависимостей:**
- Внутренние слои (Core) не знают о внешних (Presentation, Networking)
- Слои могут зависеть только от внутренних слоев
- Unity-специфичный код только в Presentation Layer

---

## ЭТАП 3. ПРОВЕРКА НА ТРЕБОВАНИЯ

### 3.1 Замена локальной игры на сетевую

**✅ ДА, возможно без переписывания логики:**

**Архитектура поддерживает:**
1. **GameState и Actions независимы от источника:**
   - `IAction` - это просто данные (Player, UnitId, Type, Parameters)
   - Не важно, откуда Action: HumanPlayer, AIPlayer или NetworkPlayer
   - `GameController.ProcessAction()` работает одинаково для всех

2. **Player - абстракция:**
   - `IPlayer.RequestAction()` - единый интерфейс
   - `HumanPlayer`, `AIPlayer`, `NetworkPlayer` - разные реализации
   - `GameController` не знает, какой тип игрока

3. **Сетевая реализация:**
   - `NetworkPlayer` получает Action от сети
   - `NetworkServer` получает Action от клиента, валидирует, выполняет
   - `NetworkClient` отправляет Action на сервер, получает обновленный GameState

**Как переключиться:**
```
Локальная игра:
  players = [HumanPlayer(id=1), AIPlayer(id=2)]
  
Сетевая игра:
  players = [NetworkPlayer(id=1, clientId=1), NetworkPlayer(id=2, clientId=2)]
  NetworkServer обрабатывает Actions от обоих клиентов
```

**Логика не меняется**, меняется только источник Actions.

---

### 3.2 Добавление нового типа игрока

**✅ ДА, легко расширяется:**

**Пример: RemoteAI (AI на удаленном сервере):**
1. Создать `RemoteAIPlayer : IPlayer`
2. Реализовать `RequestAction()` - отправляет запрос на удаленный AI сервис
3. Подключить к `GameController` - работает как обычный игрок

**Архитектура поддерживает:**
- `IPlayer` - открытый интерфейс для расширения
- `GameController` работает с любым `IPlayer`
- Не нужно менять логику боя

---

### 3.3 Поддержка будущих расширений

#### **Способности с кулдаунами (Abilities)**
**✅ Уже заложено:**
- `IAbility` определяет базовые параметры способности
- `IAbilityInstance` отслеживает кулдауны для каждого юнита
- `IUnit.Abilities` возвращает список способностей юнита
- `ActionValidator` проверяет `IsAvailable` перед использованием способности

**Реализация:**
- Кулдауны декрементируются в конце хода (в `GameController` или `ActionExecutor`)
- При использовании способности кулдаун сбрасывается на `Ability.CooldownDuration`
- `GetAvailableAbilities()` возвращает только способности с `CurrentCooldown == 0`

**Примеры способностей:**
- Атака ближнего боя (cooldown: 0 - доступна каждый ход)
- Мощная атака (cooldown: 2 - доступна раз в 3 хода)
- Исцеление (cooldown: 3 - доступна раз в 4 хода)

---

#### **Новые типы win conditions**
**✅ Легко расширяется:**
- `IWinCondition` - открытый интерфейс
- `GameController` проверяет все зарегистрированные win conditions

**Примеры:**
- `EliminateAllEnemiesWinCondition`
- `ReachObjectiveWinCondition`
- `SurviveTurnsWinCondition`

**Реализация:**
- Создать класс, реализующий `IWinCondition`
- Зарегистрировать в `GameController`

---


---

### 3.4 Потенциальные архитектурные риски

#### **1. Tight Coupling между GameState и Actions**
**Риск:** GameState знает о конкретных типах Actions

**Митигация:**
- `IAction` - абстрактный интерфейс
- `GameState.ApplyAction()` работает через `ActionExecutor`
- Конкретные Actions в отдельных классах

---

#### **2. Нарушение Single Responsibility в GameController**
**Риск:** GameController делает слишком много (оркестрация + валидация + выполнение)

**Митигация:**
- Разделить ответственности:
  - `GameController` - оркестрация
  - `ActionValidator` - валидация
  - `ActionExecutor` - выполнение
  - `TurnManager` - управление ходами

---

#### **3. Проблемы с синхронизацией в мультиплеере**
**Риск:** Race conditions, десинхронизация состояния

**Митигация:**
- **Server-authoritative:** Сервер - единственный источник правды
- **Deterministic execution:** Все Actions выполняются детерминированно
- **State synchronization:** Сервер периодически синхронизирует GameState
- **Action validation on server:** Клиент отправляет намерение, сервер валидирует и выполняет

**Архитектура:**
```
Client → Action → NetworkServer → ActionValidator → ActionExecutor → GameState
                                                                    ↓
                                                              Broadcast State
                                                                    ↓
                                                              All Clients
```

---

#### **4. Unity-зависимость в Core Layer**
**Риск:** Использование Unity-типов (Vector3, MonoBehaviour) в Core

**Митигация:**
- Core Layer использует только стандартные типы C#
- Для позиций использовать `HexCoordinates` (уже есть)
- Если нужны Vector3 - создать обертку или использовать структуру без Unity

---

## ЭТАП 4. ПЛАН РЕАЛИЗАЦИИ (БЕЗ КОДА)

### Фаза 1: Core Layer (Combat/Core/)
**Goal:** Создать фундамент - GameState, Unit, Action, Ability

**Steps:**
1. Создать `IHasHealth` интерфейс (CurrentHP, MaxHP, IsAlive)
2. Создать enums:
   - `UnitActionState` (Ready, ActedThisTurn, Stunned, Dead)
   - `AbilityTargetType` (Enemy, Ally, Self, Position, Area)
   - `AbilityEffectType` (Damage, Heal, StatusEffect, Hybrid)
   - `StatusEffectType` (Buff, Debuff, DOT, HOT, Control)
3. Создать `AbilityTarget` struct (Type, TargetUnitId?, TargetPosition?)
4. Создать `IAbility` и ability subtypes:
   - `IDamageAbility` (с полем Damage)
   - `IHealAbility` (с полем HealAmount)
   - `IStatusEffectAbility` (с полем EffectToApply)
5. Создать `IAbilityInstance` и `AbilityInstance` (способности с кулдаунами)
6. Создать `ScheduledAbility` struct (Ability, Target, ExecutionOrder)
7. Создать `IStatusEffect` и базовые реализации (Poison, Regen, Stun)
8. Создать `IUnit` и `Unit`:
   - Расширяет `IHasHealth`
   - Содержит `AbilityQueue` (список ScheduledAbility)
   - Содержит `HasActedThisTurn` и `ActionState`
9. Создать конкретные Action классы (вместо dictionary):
   - Turn-Ending: `MoveAction`, `ExecuteAbilityQueueAction`, `EndUnitTurnAction`
   - Non-Turn-Ending: `ScheduleAbilityAction`, `ReorderAbilitiesAction`, `RetargetAbilityAction`
10. Создать `IGameState` и `GameState` (IMMUTABLE структура)
11. Создать `IWinCondition` и `EliminateAllEnemiesWinCondition`

**Dependencies:** Только от существующего `Battlefield` (для `HexCoordinates`)

**Can be written independently:** Да, это pure C# без Unity

---

### Фаза 2: Turn Management (Combat/TurnManagement/)
**Goal:** Управление очередностью ходов

**Steps:**
1. Создать `ITurnManager` и `TurnManager`
2. Реализовать простой round-robin порядок ходов
3. Интегрировать с `GameState` (CurrentPlayer)

**Dependencies:** Фаза 1 (Unit, GameState)

**Can be written independently:** Да, после Фазы 1

---

### Фаза 3: Action Execution (Combat/Execution/)
**Goal:** Выполнение и валидация Actions

**Steps:**
1. Создать `ValidationResult` (Success, FailureReason, Details)
2. Создать `IActionValidator` и `ActionValidator`
   - Валидация прав игрока
   - Валидация состояния юнита (CanAct, HasActedThisTurn)
   - Валидация кулдаунов способностей
   - Валидация целей (range, line of sight)
   - Валидация лимитов очереди (максимум 1 новая способность за ход)
3. Создать `IDamageSystem` и `DamageSystem`
   - `ApplyDamage(gameState, target, amount)` - возвращает новый GameState
   - `ApplyHealing(gameState, target, amount)` - возвращает новый GameState
4. Создать `IAbilityExecutor` и `AbilityExecutor`
   - `ExecuteAbility(gameState, caster, scheduledAbility)` - выполняет одну способность
   - Интегрируется с `DamageSystem` для урона/лечения
   - Применяет StatusEffects от способностей
5. Создать `IActionExecutor` и `ActionExecutor`
   - Реализует выполнение для каждого типа Action
   - `Execute(MoveAction)` - перемещение юнита
   - `Execute(ScheduleAbilityAction)` - добавление в очередь
   - `Execute(ExecuteAbilityQueueAction)` - выполнение очереди через AbilityExecutor
   - `Execute(ReorderAbilitiesAction)` - изменение порядка
   - `Execute(RetargetAbilityAction)` - изменение цели
   - `Execute(EndUnitTurnAction)` - завершение хода юнита
   - Все методы возвращают новый immutable GameState
6. Реализовать управление состоянием юнита:
   - Установка `HasActedThisTurn = true` для turn-ending actions
   - Декремент кулдаунов в конце хода
   - Применение StatusEffects (начало/конец хода)

**Dependencies:** Фаза 1 (Actions, Abilities, StatusEffects), Фаза 2 (TurnManager)

**Can be written independently:** Частично - валидацию и систему урона можно писать параллельно

---

### Фаза 4: Game Controller (Combat/Controller/)
**Goal:** Оркестрация игрового цикла

**Steps:**
1. Создать `IGameController` и `GameController`
2. Интегрировать `TurnManager`, `ActionExecutor`, `ActionValidator`
3. Реализовать игровой цикл:
   - Начало хода: сброс `HasActedThisTurn` для всех юнитов текущего игрока
   - Декремент кулдаунов способностей
   - Применение StatusEffects (начало хода)
   - Обработка Actions: валидация → выполнение → новый GameState
   - Проверка автоматического окончания хода (все юниты HasActedThisTurn == true)
   - Применение StatusEffects (конец хода)
   - Проверка win conditions
   - Переход к следующему игроку
4. Добавить проверку win conditions после каждого действия
5. Реализовать переходы между GamePhase (Setup → Combat → Victory/Defeat)

**Dependencies:** Фазы 1, 2, 3

**Can be written independently:** Нет, зависит от всех предыдущих

---

### Фаза 5: Player Layer - Human (Player/Human/)
**Goal:** Человеческий игрок с Unity input

**Steps:**
1. Создать `IPlayer` и `HumanPlayer`
2. Создать `HumanPlayerController` (MonoBehaviour для input)
3. Интегрировать с Unity Input System
4. Создавать Actions из input (MoveAction из WASD, AttackAction из клика)

**Dependencies:** Фаза 1 (Actions), Фаза 4 (GameController)

**Can be written independently:** Да, после Фазы 1

---

### Фаза 6: Player Layer - AI (Player/AI/)
**Goal:** AI игрок

**Steps:**
1. Создать `AIPlayer` и `IAIDecisionMaker`
2. Реализовать простой AI (`SimpleAIDecisionMaker`):
   - Выбор случайного валидного действия
   - Или выбор действия с максимальным уроном
3. Интегрировать с `GameController`

**Dependencies:** Фаза 1 (Actions), Фаза 4 (GameController)

**Can be written independently:** Да, после Фазы 1

---

### Фаза 7: Presentation Layer (Combat/View/)
**Goal:** Визуализация в Unity

**Steps:**
1. Создать `UnitView` (MonoBehaviour для отображения юнита)
2. Создать `GameStateView` (MonoBehaviour для синхронизации с GameState)
3. Создать `ActionPreviewView` (предпросмотр действий)
4. Интегрировать с существующим `BattlefieldView` (отображение hex grid)

**Dependencies:** Фаза 1 (GameState, Unit), существующий Battlefield

**Can be written independently:** Частично - можно начать после Фазы 1, но полная интеграция после Фазы 4

---

### Фаза 8: Networking Layer (Combat/Networking/)
**Goal:** Сетевое взаимодействие с Netcode for GameObjects

**Steps:**
1. Создать `ActionSerializer` (сериализация Actions в JSON/Binary)
2. Создать `GameStateSerializer` (сериализация GameState)
3. Создать `NetworkGameStateSync` (NetworkBehaviour):
   - `[ServerRpc]` для приема Actions от клиентов
   - `[ClientRpc]` для синхронизации GameState клиентам
4. Создать `NetworkActionSender` (NetworkBehaviour):
   - Отправка Actions на сервер через `[ServerRpc]`
5. Создать `NetworkPlayer` (реализация IPlayer для сети)
6. Интегрировать с `GameController`
7. Настроить Netcode NetworkManager (отключить ненужную синхронизацию)

**Dependencies:** Фаза 1 (Actions, GameState), Фаза 4 (GameController)

**Can be written independently:** Частично - сериализацию можно писать параллельно с другими фазами

**Важно:** Использовать только RPC, не использовать NetworkTransform (не нужен для turn-based)

---

### Фаза 9: Интеграция с существующей системой
**Goal:** Подключить к существующим Platform и Battlefield

**Steps:**
1. Создать `CombatBattlefield` (обертка над существующим `IBattlefield`)
2. Адаптировать `CombatPlatform` для использования `GameController`
3. Интегрировать `Battlefield` с позиционированием юнитов
4. Создать переход от Platform navigation к Combat mode
5. Тестирование полного цикла

**Dependencies:** Все предыдущие фазы, существующий код

**Can be written independently:** Нет, зависит от всех фаз

---

### Фаза 10: Расширения (StatusEffects, дополнительные Win Conditions)
**Goal:** Добавить продвинутые функции

**Steps:**
1. Добавить систему статусов (StatusEffects)
2. Добавить дополнительные win conditions (если понадобятся)
3. Расширить систему способностей (если понадобится)

**Dependencies:** Фазы 1-4

**Can be written independently:** Да, после базовых фаз

**Note:** EliminateAllEnemiesWinCondition уже реализован в Фазе 1

---

## ЭТАП 5. КРАТКОЕ РЕЗЮМЕ

### Архитектура в двух словах:

**Чистая архитектура с разделением на слои:**
- **Core Layer** - чистая логика (GameState, Unit, Action) - НЕ зависит от Unity/сети
- **Combat Layer** - управление боем (TurnManager, ActionExecutor) - зависит только от Core
- **Player Layer** - абстракция игроков (Human/AI/Network) - зависит от Core и Combat
- **Networking Layer** - сетевое взаимодействие - зависит от Core и Combat
- **Presentation Layer** - Unity визуализация - зависит от всех остальных

**Ключевые принципы:**
1. **GameState** - единый источник правды (immutable)
2. **Action/Command Pattern** - все изменения через Actions
3. **Server-authoritative** - сервер валидирует и выполняет Actions
4. **Player abstraction** - Human/AI/Network - одинаковый интерфейс
5. **Deterministic execution** - одинаковый результат на всех клиентах

**Расширяемость:**
- Новые типы Actions - добавить класс, реализующий IAction
- Новые типы игроков - добавить класс, реализующий IPlayer
- Новые win conditions - добавить класс, реализующий IWinCondition
- Новые способности - создать классы, реализующие IAbility
- StatusEffects - система эффектов для расширения

**Мультиплеер:**
- Локальная игра и сетевая игра используют одну логику
- Разница только в источнике Actions (HumanPlayer vs NetworkPlayer)
- Сервер авторитетен, клиенты отправляют намерения

---

## АНАЛИЗ NETCODE FOR GAMEOBJECTS

### Подходит ли Netcode for GameObjects для turn-based тактической игры?

**✅ ДА, но с оговорками:**

### Преимущества:

1. **Официальная поддержка Unity:**
   - Активно развивается Unity
   - Хорошая документация
   - Интеграция с Unity Editor

2. **Server-authoritative архитектура:**
   - Встроенная поддержка серверного авторитета
   - `NetworkBehaviour` с `[ServerRpc]` и `[ClientRpc]`
   - Автоматическая синхронизация `NetworkObject`

3. **Простота использования:**
   - Минимальная настройка для базовых случаев
   - Автоматическая синхронизация трансформаций через `NetworkTransform`
   - Встроенная система спавна объектов

4. **Производительность:**
   - Оптимизирован для Unity
   - Поддержка компрессии данных
   - Эффективная сериализация

### Ограничения и проблемы для turn-based игр:

1. **Избыточность для turn-based:**
   - `NetworkTransform` синхронизирует позиции каждый кадр - не нужно для пошаговой игры
   - Много автоматической синхронизации, которая не нужна
   - Можно отключить, но это добавляет сложности

2. **Архитектурное несоответствие:**
   - Netcode ориентирован на real-time игры (FPS, action games)
   - Turn-based игры требуют другого подхода:
     - Клиент отправляет Action → Сервер валидирует → Сервер выполняет → Сервер отправляет обновленный State
   - Netcode больше про синхронизацию объектов, чем про Command Pattern

3. **GameState синхронизация:**
   - Netcode синхронизирует отдельные объекты (`NetworkObject`)
   - Для turn-based нужна синхронизация всего `GameState` целиком
   - Придется делать кастомную синхронизацию через RPC

4. **Сложность с чистой архитектурой:**
   - `NetworkBehaviour` наследуется от `MonoBehaviour`
   - Нарушает принцип "Core не зависит от Unity"
   - Придется создавать адаптеры между Core и Netcode

### Рекомендуемый подход с Netcode:

**Гибридная архитектура:**

```
Core Layer (pure C#)
  └─> GameState, Unit, Action (НЕ зависят от Netcode)

Combat Layer (pure C#)
  └─> GameController, ActionExecutor (НЕ зависят от Netcode)

Networking Layer (Netcode адаптер)
  └─> NetworkGameStateSync (NetworkBehaviour)
      ├─> Синхронизирует GameState через [ClientRpc]
      └─> Принимает Actions через [ServerRpc]
  
  └─> NetworkActionSender (NetworkBehaviour)
      └─> Отправляет Actions на сервер через [ServerRpc]

Player Layer
  └─> NetworkPlayer
      └─> Использует NetworkActionSender для отправки Actions
```

**Структура с Netcode:**

```csharp
// Core Layer - чистый C#
public class GameState { ... }
public interface IAction { ... }

// Networking Layer - адаптер Netcode
public class NetworkGameStateSync : NetworkBehaviour
{
    [ServerRpc(RequireOwnership = false)]
    public void ReceiveActionServerRpc(ActionData actionData)
    {
        // Конвертировать ActionData в IAction
        // Передать в GameController
    }
    
    [ClientRpc]
    public void SyncGameStateClientRpc(GameStateData stateData)
    {
        // Конвертировать GameStateData в GameState
        // Обновить локальное состояние
    }
}
```

### Альтернативы:

1. **Mirror Networking:**
   - Более гибкий для кастомных протоколов
   - Лучше подходит для Command Pattern
   - Но менее официальная поддержка

2. **Custom networking:**
   - Полный контроль над протоколом
   - Идеально для turn-based (низкая частота обновлений)
   - Но больше работы по реализации

3. **Netcode for GameObjects (рекомендуется):**
   - Использовать только RPC для Actions и State
   - Отключить автоматическую синхронизацию объектов
   - Создать адаптеры между Core и Netcode

### Итоговая рекомендация:

**✅ Использовать Netcode for GameObjects, но:**

1. **Использовать только RPC:**
   - `[ServerRpc]` для отправки Actions от клиента на сервер
   - `[ClientRpc]` для синхронизации GameState от сервера клиентам
   - НЕ использовать `NetworkTransform` (не нужен для turn-based)

2. **Создать адаптеры:**
   - `NetworkGameStateSync` - синхронизирует GameState
   - `NetworkActionSender` - отправляет Actions
   - Изолировать Netcode от Core Layer

3. **Сериализация:**
   - Использовать `INetworkSerializable` для кастомных структур
   - Или JSON/Binary сериализацию для GameState и Actions

4. **Архитектура:**
   - Core и Combat остаются pure C#
   - Networking Layer - это тонкий адаптер над Netcode
   - Presentation Layer использует Netcode компоненты

**Вывод:** Netcode подходит, но нужно использовать его минимально (только RPC) и создать правильные адаптеры для изоляции Core от Unity/Netcode.

---

## ⚠️ ОЖИДАНИЕ РЕВЬЮ И ПОДТВЕРЖДЕНИЯ ПЕРЕД РЕАЛИЗАЦИЕЙ

**Следующие шаги:**
1. Ревью архитектуры
2. Обсуждение спорных моментов
3. Уточнение требований
4. **ТОЛЬКО ПОСЛЕ ПОДТВЕРЖДЕНИЯ** - переход к реализации

**Решения:**
- Реплеи: не нужны на данном этапе
- Networking: Netcode for GameObjects (см. анализ выше)
- Rollback/reconnect: не нужны на данном этапе
- Win conditions: приоритет - EliminateAllEnemiesWinCondition

---

## ИЗМЕНЕНИЯ ОТ ПЕРВОНАЧАЛЬНОГО ПЛАНА

### 1. Структура папок
- **Player**, **Networking**, **Battlefield** перемещены под папку **Combat/**
- Все компоненты turn-based combat теперь в одном месте
- Добавлены новые файлы:
  - Интерфейсы: `IHasHealth.cs`, `IAbility.cs`, `IAbilityInstance.cs`, `IStatusEffect.cs`, `IDamageAbility.cs`, `IHealAbility.cs`, `IStatusEffectAbility.cs`
  - Структуры: `ScheduledAbility.cs`, `AbilityTarget.cs`
  - Enums: `UnitActionState.cs`, `AbilityTargetType.cs`, `AbilityEffectType.cs`, `StatusEffectType.cs`
  - Системы: `DamageSystem.cs`, `IDamageSystem.cs`, `AbilityExecutor.cs`, `IAbilityExecutor.cs`
  - Конкретные Actions: `MoveAction`, `ScheduleAbilityAction`, `ExecuteAbilityQueueAction`, `ReorderAbilitiesAction`, `RetargetAbilityAction`, `EndUnitTurnAction`
- Удалены файлы: `TurnOrderCalculator.cs`, `ITurnOrderCalculator.cs`, `ActionEndsTurn.cs`

### 2. Архитектурные улучшения

**2.1. GameState теперь ЯВНО immutable:**
- Каждое изменение создает новый экземпляр
- Удален метод `ApplyAction()` из `IGameState` (это делает ActionExecutor)
- Удален метод `IsValidAction()` из `IGameState` (это делает ActionValidator)
- GameState теперь только хранит данные и предоставляет query методы

**2.2. Type-safe Actions:**
- Удален `IReadOnlyDictionary<string, object> Parameters`
- Каждый Action - отдельный класс с типизированными свойствами
- `MoveAction` имеет `HexCoordinates TargetPosition`
- `ScheduleAbilityAction` имеет `int AbilityId` и `AbilityTarget Target`
- Невозможны runtime ошибки из-за отсутствующих ключей

**2.3. Per-unit action tracking:**
- Добавлен `HasActedThisTurn` в `IUnit`
- Добавлен `UnitActionState` enum (Ready, ActedThisTurn, Stunned, Dead)
- Каждый юнит может выполнить ОДНО главное действие за ход игрока
- Поддерживается "частичный" ход - можно планировать способности, но не выполнять

**2.4. Ability queue system:**
- Добавлен `AbilityQueue` в `IUnit` (список `ScheduledAbility`)
- Максимум 1 новая способность может быть запланирована за ход
- Способности выполняются только при явной команде `ExecuteAbilityQueueAction`
- Кулдауны начинаются ПОСЛЕ выполнения, не после планирования
- Можно изменять очередь (reorder, retarget) пока юнит не действовал

**2.5. Ability execution model:**
- Добавлены ability subtypes: `IDamageAbility`, `IHealAbility`, `IStatusEffectAbility`
- Добавлен `IAbilityExecutor` для выполнения способностей
- Добавлен `IDamageSystem` для управления уроном/лечением
- Способности могут иметь несколько эффектов (урон + статус эффект)

**2.6. Status Effects полностью определены:**
- Добавлен `IStatusEffect` с Duration, StackCount, Type
- Поддержка DOT (Damage Over Time), HOT (Heal Over Time), Control (Stun)
- Эффекты применяются в начале/конце хода

### 3. Классификация Actions (ОБНОВЛЕНО)

**Turn-Ending Actions (заканчивают ход юнита):**
- `MoveAction` - перемещение
- `ExecuteAbilityQueueAction` - выполнение очереди способностей
- `EndUnitTurnAction` - явное завершение хода без действий

**Non-Turn-Ending Actions (НЕ заканчивают ход юнита):**
- `ScheduleAbilityAction` - добавление способности в очередь (макс 1 за ход)
- `ReorderAbilitiesAction` - изменение порядка в очереди
- `RetargetAbilityAction` - изменение цели способности в очереди

### 4. Удалено
- Реплеи (не нужны на данном этапе)
- Rollback/reconnect (не нужны на данном этапе)
- История действий в GameState (не нужна без реплеев)
- **AP (Action Points)** - система очков действия удалена
- **Initiative** - система инициативы удалена
- **TurnOrderCalculator** - заменен на простой round-robin
- **Dictionary Parameters в Actions** - заменено на type-safe классы

### 5. Добавлено
- **IHasHealth** - интерфейс для сущностей с HP
- **IAbility** с подтипами (IDamageAbility, IHealAbility, IStatusEffectAbility)
- **IAbilityInstance** - экземпляр способности с кулдауном
- **ScheduledAbility** - способность в очереди юнита
- **AbilityTarget** - типизированная цель способности
- **IStatusEffect** - полностью определенная система эффектов
- **IAbilityExecutor** - система выполнения способностей
- **IDamageSystem** - система урона и лечения
- **UnitActionState** - состояние юнита в течение хода
- **HasActedThisTurn** - флаг активности юнита

### 6. Изменено
- **IGameState** - явно immutable, удалены методы изменения состояния
- **IUnit** - добавлены `AbilityQueue`, `HasActedThisTurn`, `ActionState`
- **IAction** - конкретные классы вместо dictionary
- **ITurnManager** - упрощен (удален метод `CalculateTurnOrder`)
- **ActionValidator** - проверяет состояние юнита, лимиты очереди, кулдауны
- **ActionExecutor** - возвращает новый GameState (immutable), интегрирован с AbilityExecutor и DamageSystem

### 7. Приоритеты
- `EliminateAllEnemiesWinCondition` - приоритетная реализация

### 8. Networking решение
- **Netcode for GameObjects** - выбранное решение
- Подробный анализ преимуществ и ограничений в разделе "АНАЛИЗ NETCODE FOR GAMEOBJECTS"
- Рекомендация: использовать только RPC, создать адаптеры для изоляции Core

### 9. Модель хода (NEW)
- **Player Turns** - игрок управляет всеми своими юнитами за ход
- Каждый юнит может выполнить ОДНО главное действие
- Юниты могут планировать способности и изменять очередь без завершения хода
- Ход автоматически завершается, когда все юниты игрока HasActedThisTurn == true
- Или игрок может явно завершить ход (EndTurnAction)

---

**❌ КОД НЕ НАПИСАН**
**❌ РЕАЛИЗАЦИЯ НЕ НАЧАТА**
**✅ ТОЛЬКО ПЛАНИРОВАНИЕ И АРХИТЕКТУРА**

