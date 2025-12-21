# Architecture Proposal: Platform-Based Game Refactoring

## Overview
This document outlines the proposed architecture for refactoring the proof-of-concept code into a well-structured, extensible system following OOP principles and design patterns.

---

## 1. Project Structure

```
Assets/__Project/
├── Scripts/
│   ├── Core/                          # Core systems (non-domain specific)
│   │   ├── DI/                        # Zenject installers
│   │   │   ├── GameInstaller.cs
│   │   │   ├── PlatformInstaller.cs
│   │   │   ├── BattlefieldInstaller.cs
│   │   │   └── CharacterInstaller.cs
│   │   ├── Events/                    # Event system (Signals/Commands)
│   │   │   ├── PlatformEvents.cs
│   │   │   ├── CharacterEvents.cs
│   │   │   └── BattlefieldEvents.cs
│   │   └── Utils/                     # Utility classes
│   │       ├── GeometryUtils.cs
│   │       └── MathUtils.cs
│   │
│   ├── Platform/                      # Platform Domain
│   │   ├── Interfaces/
│   │   │   ├── IPlatform.cs
│   │   │   ├── IPlatformState.cs
│   │   │   ├── IPlatformContent.cs
│   │   │   └── IPlatformVisual.cs
│   │   ├── Model/                     # Platform model (logic)
│   │   │   ├── Platform.cs
│   │   │   ├── PlatformStateMachine.cs
│   │   │   └── PlatformStateTransition.cs
│   │   ├── States/                    # State Machine States
│   │   │   ├── PlatformStateBase.cs
│   │   │   ├── PlatformIdleState.cs
│   │   │   ├── PlatformActiveState.cs
│   │   │   ├── PlatformCompletedState.cs
│   │   │   └── PlatformLockedState.cs
│   │   ├── Implementations/
│   │   │   ├── SimplePlatform.cs
│   │   │   └── CombatPlatform.cs       # Has Battlefield attribute
│   │   ├── Content/                   # Platform content types
│   │   │   ├── PlatformContentBase.cs
│   │   │   ├── EnemyContent.cs
│   │   │   ├── NpcContent.cs
│   │   │   ├── LootContent.cs
│   │   │   └── QuestContent.cs
│   │   ├── Visual/                    # Platform visual data
│   │   │   ├── PlatformVisual.cs
│   │   │   ├── PlatformMeshBuilder.cs
│   │   │   └── PlatformColliderBuilder.cs
│   │   └── View/                      # Unity MonoBehaviour views
│   │       ├── PlatformView.cs
│   │       └── PlatformViewFactory.cs
│   │
│   ├── Battlefield/                   # Battlefield Domain
│   │   ├── Interfaces/
│   │   │   ├── IBattlefield.cs
│   │   │   ├── IHexGrid.cs
│   │   │   ├── IHexCell.cs
│   │   │   └── IHexCoordinates.cs
│   │   ├── Core/
│   │   │   ├── Battlefield.cs         # Encapsulates hex grid
│   │   │   ├── HexCoordinates.cs
│   │   │   ├── HexOrientation.cs      # Enum: Flat, Pointy
│   │   │   └── HexLayout.cs
│   │   ├── Grid/
│   │   │   ├── HexGridBase.cs
│   │   │   ├── FlatHexGrid.cs
│   │   │   └── PointyHexGrid.cs
│   │   ├── Cell/
│   │   │   ├── HexCell.cs
│   │   │   └── HexCellData.cs
│   │   ├── Generation/
│   │   │   ├── HexGridGenerator.cs
│   │   │   └── HexGridPlacementStrategy.cs
│   │   └── View/
│   │       ├── HexCellView.cs
│   │       └── HexGridRenderer.cs
│   │
│   ├── LevelGeneration/               # Level Generation Domain
│   │   ├── Interfaces/
│   │   │   ├── IScenarioGenerator.cs
│   │   │   ├── IPlatformGraphGenerator.cs
│   │   │   ├── IAreaGenerator.cs
│   │   │   └── IPlatformObjectPool.cs
│   │   ├── Scenario/
│   │   │   ├── ScenarioGenerator.cs
│   │   │   ├── GameContext.cs         # Character experience, progress, etc.
│   │   │   └── ScenarioData.cs
│   │   ├── Graph/
│   │   │   ├── PlatformGraphGenerator.cs
│   │   │   ├── GraphNode.cs
│   │   │   ├── GraphEdge.cs
│   │   │   └── NodeType.cs            # Enum: KeyPlatform, FillerPlatform
│   │   ├── Area/
│   │   │   ├── AreaGenerator.cs
│   │   │   ├── PerlinNoiseMap.cs
│   │   │   ├── PlacementStrategy.cs
│   │   │   ├── LandscapeFormer.cs
│   │   │   └── PlatformObjectPool.cs
│   │   ├── Builder/                   # Builder Pattern
│   │   │   ├── PlatformGraphBuilder.cs
│   │   │   ├── PlatformDefinitionBuilder.cs
│   │   │   └── PlatformBuilder.cs
│   │   └── View/
│   │       └── AreaView.cs            # MonoBehaviour for scene
│   │
│   ├── Character/                     # Character Domain
│   │   ├── Interfaces/
│   │   │   ├── ICharacterController.cs
│   │   │   └── ICharacterMovement.cs
│   │   ├── Movement/
│   │   │   ├── CharacterMovement.cs
│   │   │   ├── CharacterDash.cs
│   │   │   └── PlatformNavigation.cs
│   │   └── View/
│   │       └── CharacterControllerView.cs
│   │
│   ├── Enemy/                         # Enemy Domain (placeholder)
│   │   ├── Interfaces/
│   │   │   └── IEnemy.cs
│   │   └── Implementations/
│   │       └── EnemyBase.cs
│   │
│   ├── Inventory/                     # Inventory Domain (placeholder)
│   │   ├── Interfaces/
│   │   │   └── IInventory.cs
│   │   └── Implementations/
│   │       └── Inventory.cs
│   │
│   └── Demo/                          # Old POC code (to be deprecated)
│       └── ...
│
├── Resources/
│   ├── Prefabs/
│   │   ├── Platform/
│   │   │   └── PlatformBase.prefab
│   │   ├── Battlefield/
│   │   │   └── HexCell.prefab
│   │   └── Character/
│   │       └── Character.prefab
│   └── Materials/
│
└── Scenes/
    ├── Demo.unity                     # Old demo scene
    └── AreaGenerationTest.unity       # New test scene
```

---

## 2. Core Interfaces & Dependencies

### 2.1 Platform Domain

#### IPlatform
```csharp
public interface IPlatform
{
    int Id { get; }
    
    // Model (logic)
    PlatformStateMachine StateMachine { get; }
    IReadOnlyList<IPlatformContent> Contents { get; }
    IReadOnlyList<IPlatform> Neighbors { get; }
    
    // Visual (geometry)
    IPlatformVisual Visual { get; }
    
    void Initialize(IPlatformVisual visual);
    void Enter();
    void Exit();
    void AddNeighbor(IPlatform platform);
    void AddContent(IPlatformContent content);
}
```

#### IPlatformVisual
```csharp
public interface IPlatformVisual
{
    Vector3 Position { get; set; }
    Vector2 Size { get; set; }
    List<Vector3> TopBoundary { get; set; }
    
    void UpdateVisual();
}
```

#### IPlatformState
```csharp
public interface IPlatformState
{
    void OnEnter(IPlatform platform);
    void OnUpdate(IPlatform platform);
    void OnExit(IPlatform platform);
    bool CanTransitionTo(IPlatformState targetState);
}
```

#### IPlatformContent
```csharp
public interface IPlatformContent
{
    ContentType Type { get; }
    void OnPlatformEntered(IPlatform platform);
    void OnPlatformExited(IPlatform platform);
}
```

**Platform Graph Structure:**
- Platforms form a directed graph (not a List)
- One entry platform (has no incoming edges, identified by `PlatformGraphData.EntryNodeId`)
- Navigation via `IReadOnlyList<IPlatform> Neighbors` property
- Character progresses through graph by moving to neighbor platforms
- No direct List manipulation - all access through graph structure

**Dependencies:**
- `IPlatform` → `PlatformStateMachine` → `IPlatformState`
- `IPlatform` → `IReadOnlyList<IPlatformContent>` (multiple contents)
- `IPlatform` → `IReadOnlyList<IPlatform> Neighbors` (graph connections)
- `IPlatform` → `IPlatformVisual` (separated visual data)
- `CombatPlatform` → `IBattlefield` (has Battlefield attribute, initialized in Initialize())

---

### 2.2 Battlefield Domain

#### IBattlefield
```csharp
public interface IBattlefield
{
    bool IsActive { get; }
    
    void Initialize(List<Vector3> boundary, Vector3 center, float hexSize, HexOrientation orientation);
    void Activate();
    void Deactivate();
    void Clear();
    
    // Grid access through Battlefield interface (encapsulated)
    IHexCell GetCellAt(HexCoordinates coordinates);
    IReadOnlyList<IHexCell> GetCellsInRange(HexCoordinates center, int range);
    IReadOnlyList<HexCoordinates> GetCellsInBoundary();
    Vector3 HexToWorld(HexCoordinates hex);
    HexCoordinates WorldToHex(Vector3 world);
    bool IsCellInBoundary(HexCoordinates hex);
}
```

#### IHexGrid
```csharp
public interface IHexGrid
{
    HexOrientation Orientation { get; }
    float HexSize { get; }
    List<Vector3> Boundary { get; }
    
    void Initialize(List<Vector3> boundary, Vector3 center, float hexSize);
    List<HexCoordinates> GetCellsInBoundary();
    Vector3 HexToWorld(HexCoordinates hex);
    HexCoordinates WorldToHex(Vector3 world);
    bool IsCellInBoundary(HexCoordinates hex);
    void Clear();
}
```

#### IHexCell
```csharp
public interface IHexCell
{
    HexCoordinates Coordinates { get; }
    Vector3 WorldPosition { get; }
    Color Color { get; set; }
    bool IsActive { get; set; }
}
```

**Dependencies:**
- `IBattlefield` encapsulates `IHexGrid` (grid not exposed directly)
- `Battlefield` implements `IBattlefield` and contains private `IHexGrid`
- `IHexGrid` → `HexCoordinates`, `HexOrientation`
- `FlatHexGrid` / `PointyHexGrid` implement `IHexGrid`

---

### 2.3 Level Generation Domain

#### IAreaGenerator
```csharp
public interface IAreaGenerator
{
    IPlatform EntryPlatform { get; }
    IPlatformObjectPool ObjectPool { get; }
    
    void Generate();
    void Clear();
}

// AreaGenerator constructor takes:
// - PlatformGraphData graph
// - PerlinNoiseMap noiseMap
// - IPlatformObjectPool objectPool (optional, creates default if null)
```

#### IPlatformObjectPool
```csharp
public interface IPlatformObjectPool
{
    void PoolPlatform(IPlatform platform);
    void UnpoolPlatform(IPlatform platform);
    IReadOnlyList<IPlatform> ActivePlatforms { get; }
    
    void SetVisibleRange(IPlatform centerPlatform, int forwardCount, int backwardCount);
    void UpdateVisiblePlatforms(IPlatform currentPlatform);
}
```

#### IScenarioGenerator
```csharp
public interface IScenarioGenerator
{
    ScenarioData GenerateScenario(GameContext context);
}
```

#### IPlatformGraphGenerator
```csharp
public interface IPlatformGraphGenerator
{
    PlatformGraphData GenerateGraph(ScenarioData scenario);
}
```

**Dependencies:**
- `IAreaGenerator` constructor takes `PlatformGraphData` and `PerlinNoiseMap`
- `AreaGenerator` creates all platform game objects and configures them
- `AreaGenerator` uses `IPlatformObjectPool` to manage platform visibility
- Platforms form a graph with one entry platform (has no incoming edges)
- `AreaGenerator.EntryPlatform` returns the entry platform of the graph

---

### 2.4 Builder Pattern

#### PlatformGraphBuilder
```csharp
public class PlatformGraphBuilder
{
    private List<IPlatformDefinitionBuilder> definitionBuilders = new();
    
    public PlatformGraphBuilder WithPlatform(IPlatformDefinitionBuilder builder) { ... }
    public PlatformGraphData Build() { ... }  // Builds graph data, not instances
}
```

#### IPlatformDefinitionBuilder
```csharp
public interface IPlatformDefinitionBuilder
{
    IPlatformDefinitionBuilder WithContent(PlatformContentType contentType);
    PlatformDefinition Build();  // Returns definition data, not IPlatform instance
}
```

#### IPlatformBuilder
```csharp
public interface IPlatformBuilder
{
    IPlatform Build(PlatformDefinition definition, IPlatformVisual visual);
}
```

**Dependencies:**
- `PlatformGraphBuilder` → `IPlatformDefinitionBuilder` → `PlatformDefinition` (builds graph data)
- `PlatformBuilder` → `IPlatform` (builds platform instances from definitions)
- `PlatformDefinition` contains content types and metadata (data only)
- `AreaGenerator` uses `PlatformGraphData` to build the actual area in 3D scene

---

## 3. Class Relationships Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Level Generation                          │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  AreaGenerator                                               │
│    ├─> Constructor: PlatformGraphData, PerlinNoiseMap       │
│    ├─> Generate() ──> Creates all platform GameObjects      │
│    ├─> EntryPlatform (graph entry point)                    │
│    └─> IPlatformObjectPool                                   │
│         ├─> PoolPlatform() / UnpoolPlatform()               │
│         └─> SetVisibleRange() - shows n forward, m backward │
│                                                               │
│  PlatformGraphBuilder                                        │
│    └─> IPlatformDefinitionBuilder[] ──> PlatformGraphData   │
│         (builds graph data, not instances)                   │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      Platform Domain                         │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  IPlatform                                                   │
│    ├─> PlatformStateMachine (Model)                          │
│    │     └─> IPlatformState (Idle, Active, Completed...)   │
│    ├─> IReadOnlyList<IPlatformContent> Contents (Model)      │
│    ├─> IReadOnlyList<IPlatform> Neighbors (Model)            │
│    └─> IPlatformVisual (Visual)                              │
│         ├─> Position, Size, TopBoundary                      │
│                                                               │
│  Implementations:                                            │
│    ├─> SimplePlatform                                        │
│    └─> CombatPlatform                                        │
│         └─> IBattlefield Battlefield (initialized in        │
│             Initialize())                                    │
│                                                               │
│  PlatformView (MonoBehaviour) ──> IPlatform                  │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                    Battlefield Domain                       │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  IBattlefield                                                │
│    ├─> GetCellAt(), GetCellsInRange(), etc.                 │
│    └─> IHexGrid (encapsulated, not exposed)                  │
│         ├─> HexCoordinates                                   │
│         └─> HexOrientation (Flat/Pointy)                    │
│                                                               │
│  Implementations:                                            │
│    ├─> Battlefield (contains private IHexGrid)              │
│    ├─> FlatHexGrid                                          │
│    └─> PointyHexGrid                                        │
│                                                               │
│  HexGridRenderer (MonoBehaviour) ──> IBattlefield            │
└─────────────────────────────────────────────────────────────┘
```

---

## 4. Zenject Dependency Injection Structure

### 4.1 Installers

**GameInstaller.cs** (Main installer)
```csharp
public class GameInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Core systems
        Container.Bind<IScenarioGenerator>().To<ScenarioGenerator>().AsSingle();
        Container.Bind<IPlatformGraphGenerator>().To<PlatformGraphGenerator>().AsSingle();
        
        // Object Pool
        Container.Bind<IPlatformObjectPool>().To<PlatformObjectPool>().AsSingle();
        
        // AreaGenerator - created with constructor injection
        // Container.Bind<IAreaGenerator>().To<AreaGenerator>();
        // Note: AreaGenerator constructor takes:
        //   - PlatformGraphData graph
        //   - PerlinNoiseMap noiseMap
        //   - IPlatformObjectPool objectPool (optional)
        // Create instance manually with these parameters
        
        // Factory Registry
        Container.Bind<IPlatformFactoryRegistry>().To<PlatformFactory>().AsSingle();
        
        // Platform Factories (registered in PlatformInstaller)
        Container.BindFactory<SimplePlatform, SimplePlatform.Factory>();
        Container.BindFactory<CombatPlatform, CombatPlatform.Factory>();
        
        // Battlefield and Grid Factories
        Container.BindFactory<IBattlefield, BattlefieldFactory>().FromFactory<BattlefieldFactory>();
        Container.BindFactory<IHexGrid, HexGridFactory>().FromFactory<HexGridFactory>();
    }
}
```

**PlatformInstaller.cs**
```csharp
public class PlatformInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Platform factories
        Container.BindFactory<SimplePlatform, SimplePlatform.Factory>();
        Container.BindFactory<CombatPlatform, CombatPlatform.Factory>();
        
        // Register factories in registry
        var registry = Container.Resolve<IPlatformFactoryRegistry>();
        registry.RegisterFactory(PlatformType.Simple, Container.Resolve<SimplePlatform.Factory>());
        registry.RegisterFactory(PlatformType.Combat, Container.Resolve<CombatPlatform.Factory>());
    }
}
```

**BattlefieldInstaller.cs**
```csharp
public class BattlefieldInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindFactory<FlatHexGrid, FlatHexGrid.Factory>();
        Container.BindFactory<PointyHexGrid, PointyHexGrid.Factory>();
    }
}
```

### 4.2 Factories

**PlatformFactory.cs** (Factory Registry Pattern)
```csharp
public interface IPlatformFactoryRegistry
{
    void RegisterFactory(PlatformType type, IFactory<IPlatform> factory);
    IPlatform Create(PlatformType type);
}

public class PlatformFactory : IPlatformFactoryRegistry
{
    private readonly Dictionary<PlatformType, IFactory<IPlatform>> factories = new();
    
    public void RegisterFactory(PlatformType type, IFactory<IPlatform> factory)
    {
        factories[type] = factory;
    }
    
    public IPlatform Create(PlatformType type)
    {
        if (!factories.TryGetValue(type, out var factory))
            throw new ArgumentException($"No factory registered for platform type: {type}");
        return factory.Create();
    }
}
```

**Registration in Installer:**
```csharp
var platformFactory = Container.Resolve<PlatformFactory>();
platformFactory.RegisterFactory(PlatformType.Simple, Container.Resolve<SimplePlatform.Factory>());
platformFactory.RegisterFactory(PlatformType.Combat, Container.Resolve<CombatPlatform.Factory>());
```

---

## 5. State Machine Pattern for Platforms

### 5.1 State Machine Structure

```csharp
public class PlatformStateMachine
{
    private IPlatformState currentState;
    private IPlatform owner;
    
    public void Initialize(IPlatform platform, IPlatformState initialState)
    {
        owner = platform;
        ChangeState(initialState);
    }
    
    public void ChangeState(IPlatformState newState)
    {
        if (currentState != null && !currentState.CanTransitionTo(newState))
            return;
            
        currentState?.OnExit(owner);
        currentState = newState;
        currentState?.OnEnter(owner);
    }
    
    public void Update() => currentState?.OnUpdate(owner);
}
```

### 5.2 Platform States

- **PlatformIdleState**: Platform is inactive, waiting for player
- **PlatformActiveState**: Player is on platform, content is active
- **PlatformCompletedState**: Platform content completed (enemy defeated, quest done, etc.)
- **PlatformLockedState**: Platform is locked (not accessible yet)

**State Transitions:**
- Idle → Active (on player enter)
- Active → Completed (on content completion)
- Active → Idle (on player exit, if not completed)
- Any → Locked (if prerequisites not met)

### 5.3 Platform Model vs Visual Separation

**Platform Model** (logic):
- `PlatformStateMachine` - state management
- `IReadOnlyList<IPlatformContent> Contents` - content (NPC, Enemy, Loot, etc.)
- `IReadOnlyList<IPlatform> Neighbors` - graph connections

**Platform Visual** (geometry):
- `Vector3 Position` - world position
- `Vector2 Size` - platform dimensions
- `List<Vector3> TopBoundary` - polygon outline

**CombatPlatform Initialization:**
- `CombatPlatform.Initialize()` creates and initializes `IBattlefield` if needed
- Battlefield is created based on platform visual boundary

This separation allows:
- Model logic to be tested independently
- Visual updates without affecting state
- Multiple visual representations of the same platform
- Encapsulated access to battlefield grid through IBattlefield interface

---

## 6. Level Generation Phases

### Phase 1: Scenario Generation
**Input:** `GameContext` (character level, progress, story state)  
**Output:** `ScenarioData` (required platform types, difficulty, theme)

```csharp
public class ScenarioData
{
    public List<PlatformRequirement> RequiredPlatforms;
    public int DifficultyLevel;
    public LevelTheme Theme;
    public int EstimatedPlatformCount;
}
```

### Phase 2: Platform Graph Generation
**Input:** `ScenarioData`  
**Output:** `PlatformGraphData` (nodes with platform types, edges)

```csharp
public class PlatformGraphData
{
    public List<GraphNode> Nodes;  // Key platforms with types
    public List<GraphEdge> Edges;  // Connections
    public int EntryNodeId { get; }  // Node with no incoming edges (entry platform)
}

public class GraphNode
{
    public int Id;
    public PlatformType Type;
    public List<PlatformContentType> ContentTypes;  // Multiple content types
    public bool IsKeyPlatform;  // vs filler
}

public class PlatformDefinition
{
    public int Id;
    public PlatformType Type;
    public List<PlatformContentType> ContentTypes;
    // ... other metadata for platform creation
}
```

### Phase 3: Area Generation
**Input:** `PlatformGraphData` (with entry platform), `PerlinNoiseMap`  
**Output:** All platform game objects created and configured

**AreaGenerator Responsibilities:**
- Constructor takes `PlatformGraphData` and `PerlinNoiseMap`
- `Generate()` method creates all platform game objects
- Uses Perlin noise to determine height variations and positions
- Places key platforms first, fills gaps with filler platforms
- Forms natural landscape
- Assigns visual data (Position, Size, TopBoundary) to each platform via `IPlatformVisual`
- Creates platform instances from `PlatformDefinition` using `PlatformBuilder`
- All platforms are created but managed by `IPlatformObjectPool`
- `EntryPlatform` property returns the graph entry point (platform with no incoming edges)

**Object Pooling:**
- Platforms are stored in object pool (not in Lists)
- Initially, only entry platform and its neighbors are visible
- As character progresses, `AreaView` calls `ObjectPool.UpdateVisiblePlatforms(currentPlatform)`
- Pool shows next `n` platforms forward and last `m` platforms backward
- Platforms outside visible range are pooled (GameObjects disabled)
- Platforms in visible range are unpooled (GameObjects enabled)

---

## 7. Hex Grid Orientation Support

### HexOrientation Enum
```csharp
public enum HexOrientation
{
    Flat,    // Flat-top hexagons
    Pointy   // Pointy-top hexagons
}
```

### Implementation Strategy
- `HexLayout` class handles coordinate conversion based on orientation
- `FlatHexGrid` and `PointyHexGrid` implement `IHexGrid` with different math
- Common `HexCoordinates` class works for both orientations

---

## 8. Builder Pattern Implementation

### Usage Example
```csharp
// Build platform graph definition (data only, not instances)
var graph = new PlatformGraphBuilder()
    .WithPlatform(
        PlatformDefinitionBuilder.NewInstance()
            .WithContent(PlatformContentType.Npc)
            .WithContent(PlatformContentType.Quest)
            .Build()  // Returns PlatformDefinition
    )
    .WithPlatform(
        PlatformDefinitionBuilder.NewInstance()
            .WithContent(PlatformContentType.Enemy)
            .WithContent(PlatformContentType.Loot)
            .Build()
    )
    .Build();  // Returns PlatformGraphData (with EntryNodeId)

// Create PerlinNoiseMap
var noiseMap = new PerlinNoiseMap(seed, scale, octaves);

// AreaGenerator constructor takes PlatformGraphData and PerlinNoiseMap
var areaGenerator = new AreaGenerator(graph, noiseMap);
areaGenerator.Generate();  // Creates all platform game objects

// Get entry platform (graph entry point)
var entryPlatform = areaGenerator.EntryPlatform;

// Object pool manages visibility
var objectPool = areaGenerator.ObjectPool;
objectPool.SetVisibleRange(entryPlatform, forwardCount: 3, backwardCount: 1);
```

### Builder Structure
- `PlatformGraphBuilder`: Orchestrates multiple definition builders, builds graph data
- `PlatformDefinitionBuilder`: Builds `PlatformDefinition` (data only) with content types
- `PlatformBuilder`: Builds `IPlatform` instances from `PlatformDefinition` and `IPlatformVisual`
- Fluent interface: `.WithContent()`, `.Build()`
- `AreaGenerator`: Constructor takes `PlatformGraphData` and `PerlinNoiseMap`, `Generate()` creates all game objects
- `IPlatformObjectPool`: Manages platform visibility (pools/unpools based on character position)

### AreaView (MonoBehaviour)
```csharp
public class AreaView : MonoBehaviour
{
    private IAreaGenerator areaGenerator;
    private IPlatformObjectPool objectPool;
    private IPlatform currentPlatform;  // Character's current platform
    
    void Start()
    {
        // Initialize with visible range
        objectPool.SetVisibleRange(areaGenerator.EntryPlatform, forwardCount: 3, backwardCount: 1);
    }
    
    void OnCharacterPlatformChanged(IPlatform newPlatform)
    {
        currentPlatform = newPlatform;
        objectPool.UpdateVisiblePlatforms(newPlatform);
    }
}
```

**AreaView Responsibilities:**
- MonoBehaviour component attached to scene GameObject
- Holds reference to `IAreaGenerator` and `IPlatformObjectPool`
- Listens to character platform changes
- Calls `ObjectPool.UpdateVisiblePlatforms()` when character moves to new platform
- Manages visible range configuration (n forward, m backward)

---

## 9. Migration Strategy

1. **Phase 1**: Create new folder structure and interfaces
2. **Phase 2**: Implement Platform domain with State Machine
3. **Phase 3**: Implement Battlefield domain with hex orientation support
4. **Phase 4**: Implement Level Generation phases
5. **Phase 5**: Implement Builder pattern
6. **Phase 6**: Integrate Zenject
7. **Phase 7**: Create new test scene
8. **Phase 8**: Migrate character movement to new system
9. **Phase 9**: Deprecate old Demo code

---

## 10. Key Design Decisions

1. **Separation of Concerns**: Domain-based structure separates responsibilities
2. **Model/Visual Separation**: Platform model (StateMachine, Contents, Neighbors) separated from visual (Position, Size, TopBoundary)
3. **Dependency Inversion**: Interfaces allow easy extension and testing
4. **State Machine**: Makes platform behavior predictable and extensible
5. **Builder Pattern**: `PlatformDefinitionBuilder` builds graph data, `PlatformBuilder` builds instances, `AreaGenerator` builds area
6. **Generic Platform**: Single `Platform` class with multiple contents via `IReadOnlyList<IPlatformContent>`
7. **Battlefield Encapsulation**: `IBattlefield` encapsulates `IHexGrid`, grid not exposed directly
8. **Factory Registry**: Extensible factory pattern for platform creation
9. **Object Pooling**: Platforms managed in pool, not Lists. Only visible platforms (n forward, m backward) are active
10. **Graph-Based Navigation**: Platforms form a graph with one entry platform. Navigation via Neighbors, not Lists
11. **AreaGenerator**: Creates all platform game objects. Constructor takes graph and noise map, Generate() configures all
12. **Zenject**: Provides clean DI without MonoBehaviour coupling
13. **View Separation**: MonoBehaviour only for Unity-specific rendering/physics (AreaView manages pooling)
14. **Read-Only Collections**: `IReadOnlyList<T>` used for getters to maintain encapsulation

---

## Next Steps

1. Review and approve this architecture
2. Create folder structure
3. Implement interfaces
4. Implement core classes
5. Integrate Zenject
6. Create test scene
7. Migrate existing functionality

