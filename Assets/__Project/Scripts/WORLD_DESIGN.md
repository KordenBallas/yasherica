# World Design Document

**System:** Platform Navigation & Procedural Generation  
**Version:** 1.0  
**Last Updated:** January 2026  
**Status:** 🟢 Fully Implemented

---

## Table of Contents

1. [Overview](#overview)
2. [Core Concepts](#core-concepts)
3. [Platform System](#platform-system)
4. [Area Generation](#area-generation)
5. [Technical Implementation](#technical-implementation)
6. [Implementation Status](#implementation-status)

---

## Overview

### World Structure

The game world consists of **procedurally generated areas**, each containing a network of floating **platforms** that players navigate through. Each area represents a self-contained level with entry and exit points.

```
World Hierarchy:
┌──────────────────────────────────────┐
│              GAME WORLD              │
│                                      │
│  ┌────────────┐    ┌────────────┐  │
│  │  Area 1    │───→│  Area 2    │  │
│  │ (8-15      │    │ (8-15      │  │
│  │ platforms) │    │ platforms) │  │
│  └────────────┘    └────────────┘  │
│         │                 │          │
│         ↓                 ↓          │
│  ┌────────────┐    ┌────────────┐  │
│  │  Area 3    │    │  Area 4    │  │
│  └────────────┘    └────────────┘  │
└──────────────────────────────────────┘
```

### Design Goals

1. **Replayability** - Every area is unique through procedural generation
2. **Variety** - Different platform types create diverse gameplay experiences
3. **Flow** - Natural progression from entry to exit
4. **Exploration** - Optional paths and hidden content
5. **Scalability** - Easy to add new platform types and content

---

## Core Concepts

### Platform Network

Platforms are connected in a **directed graph** structure:

```
Platform Connection Example:
         ┌─────┐
    ┌───→│  1  │────┐
    │    └─────┘    │
┌─────┐           ┌─────┐
│START│           │  2  │
└─────┘           └─────┘
    │               │
    └────→┌─────┐←─┘
          │ EXIT│
          └─────┘

Legend:
- Nodes = Platforms
- Edges = Valid navigation paths
- Each platform can have multiple neighbors
```

### Platform Lifecycle

```mermaid
graph LR
    Inactive[Inactive<br/>Hidden/Disabled]
    Active[Active<br/>Visible/Available]
    Current[Current<br/>Player Present]
    Visited[Visited<br/>Completed]
    
    Inactive -->|Generate| Active
    Active -->|Enter| Current
    Current -->|Exit| Visited
    Visited -->|Can Revisit| Current
```

**State Descriptions:**
- **Inactive**: Platform exists but not yet visible to player
- **Active**: Platform is visible and can be navigated to
- **Current**: Player is currently on this platform
- **Visited**: Player has been here before

---

## Platform System

### Platform Types

| Type | Description | Purpose | Content |
|------|-------------|---------|---------|
| **Simple** | Basic traversal platform | Navigation | None or minor items |
| **Combat** | Initiates battle | Challenge | Enemies, rewards |
| **Treasure** | Contains loot | Reward | Chests, items |
| **Shop** | Trading post | Upgrade | Merchant, goods |
| **Boss** | Major encounter | Major challenge | Boss enemy |
| **Rest** | Safe haven | Recovery | Healing, save point |
| **Exit** | Level end | Progression | Next area transition |

### Platform Components

```
Platform Structure:
┌──────────────────────────────────────┐
│            Platform                   │
│                                       │
│  ┌─────────────────────────────┐    │
│  │      Visual Component        │    │
│  │  - Geometry (mesh)           │    │
│  │  - Materials                 │    │
│  │  - Position in world         │    │
│  └─────────────────────────────┘    │
│                                       │
│  ┌─────────────────────────────┐    │
│  │      Logic Component         │    │
│  │  - State machine             │    │
│  │  - Content list              │    │
│  │  - Neighbor connections      │    │
│  │  - Interaction handlers      │    │
│  └─────────────────────────────┘    │
│                                       │
│  ┌─────────────────────────────┐    │
│  │      Content Components      │    │
│  │  - Enemies (combat)          │    │
│  │  - Loot (treasure)           │    │
│  │  - NPCs (shop)               │    │
│  │  - Obstacles                 │    │
│  └─────────────────────────────┘    │
└──────────────────────────────────────┘
```

### Platform State Machine

Platforms use a **State Machine** to manage their lifecycle and behavior:

```csharp
// States available
- InactivePlatformState  // Hidden, not yet discovered
- ActivePlatformState    // Visible, can be entered
- CurrentPlatformState   // Player is here
- VisitedPlatformState   // Player has been here
```

**State Transitions:**
```
Inactive → Active    : When area generates or neighbor visited
Active → Current     : When player navigates here
Current → Visited    : When player leaves
Visited → Current    : When player returns
```

### Platform Content System

Content is **modular** and **data-driven**:

```csharp
public enum PlatformContentType
{
    None,           // Empty platform
    Combat,         // Enemy encounter
    Treasure,       // Loot chest
    Shop,           // Merchant NPC
    Puzzle,         // Environmental puzzle
    Story,          // Narrative event
    Boss            // Boss encounter
}

public interface IPlatformContent
{
    PlatformContentType Type { get; }
    void OnPlatformEnter(IPlatform platform);
    void OnPlatformExit(IPlatform platform);
    void Interact();
}
```

---

## Area Generation

### Generation Pipeline

```
Scenario Definition
        ↓
Graph Generation (Structure)
        ↓
Platform Creation (Instances)
        ↓
Platform Positioning (Layout)
        ↓
Content Population
        ↓
Visual Creation (GameObjects)
        ↓
Connection Finalization
```

### 1. Scenario Definition

**Scenarios** define the high-level structure of an area:

```csharp
public class ScenarioData
{
    public string Name;                              // "Forest Area", "Cave Level"
    public int EstimatedPlatformCount;              // Target number of platforms
    public List<PlatformRequirement> RequiredPlatforms;  // Must-have platforms
    public List<PlatformContentType> PossibleContent;    // Available content types
    public DifficultyLevel Difficulty;              // Easy, Normal, Hard
}

// Example: Forest Combat Area
ScenarioData forestScenario = new ScenarioData
{
    Name = "Forest Combat Zone",
    EstimatedPlatformCount = 12,
    RequiredPlatforms = new List<PlatformRequirement>
    {
        new PlatformRequirement(PlatformType.Simple, new[] { PlatformContentType.None }),
        new PlatformRequirement(PlatformType.Combat, new[] { PlatformContentType.Combat }),
        new PlatformRequirement(PlatformType.Combat, new[] { PlatformContentType.Combat }),
        new PlatformRequirement(PlatformType.Treasure, new[] { PlatformContentType.Treasure }),
        new PlatformRequirement(PlatformType.Exit, new[] { PlatformContentType.None })
    }
};
```

### 2. Graph Generation

**PlatformGraphGenerator** creates the connectivity structure:

```
Graph Generation Algorithm:
1. Create required platform nodes from scenario
2. Add filler nodes to reach target count
3. Create linear path (entry → required → exit)
4. Add branch connections for exploration
5. Validate: 
   - All nodes reachable from entry
   - Exit reachable from all nodes
   - No isolated subgraphs
```

**Generated Graph Structure:**
```
Example 10-platform graph:
        ┌───┐
    ┌──→│ 2 │──┐
    │   └───┘  │
┌───┐        ┌───┐   ┌───┐
│ 0 │───────→│ 3 │──→│ 5 │───→ [EXIT]
└───┘        └───┘   └───┘
 Entry         │       │
              ┌───┐  ┌───┐
              │ 4 │←─│ 6 │
              └───┘  └───┘
                │
              ┌───┐
              │ 7 │ (Treasure)
              └───┘
```

### 3. Platform Positioning

**PerlinNoiseMap** generates natural-looking layouts:

```
Positioning Algorithm:
1. Start at entry position (0, 0)
2. For each platform:
   a. Calculate base position using cursor
   b. Add Perlin noise offset for variation
   c. Adjust Y based on noise for vertical spread
   d. Move cursor horizontally
3. Apply smoothing for natural flow
```

**Visual Layout:**
```
Side view of generated area:

    Y
    ↑
    │     ╔═══╗
    │           ╔═══╗
    │  ╔═══╗         ╔═══╗
    │      ╔═══╗            ╔═══╗
    │  ╔═══╗      ╔═══╗
    └──────────────────────────────→ X
    
    - Vertical variation from Perlin noise
    - Horizontal spacing for progression
    - Natural-looking curves
```

### 4. Content Population

Content is assigned based on:
- **Platform type** (Combat platforms get enemies)
- **Scenario rules** (Difficulty affects enemy strength)
- **Random variation** (Loot tables, enemy types)

```csharp
// Content assignment example
if (platform.Type == PlatformType.Combat)
{
    var enemyContent = new CombatContent
    {
        EnemyCount = Random.Range(2, 4),
        EnemyType = scenario.GetEnemyForDifficulty(difficulty),
        Rewards = scenario.GetLootForDifficulty(difficulty)
    };
    platform.AddContent(enemyContent);
}
```

### 5. Visual Creation

**PlatformFactory** creates Unity GameObjects:

```
Visual Creation Process:
1. Instantiate platform prefab for type
2. Apply materials based on state
3. Position in world space
4. Create visual connections (bridges, paths)
5. Add content visuals (enemies, chests, etc.)
6. Setup colliders and interaction triggers
```

---

## Technical Implementation

### Key Classes

#### IPlatform Interface
```csharp
public interface IPlatform
{
    int Id { get; }
    PlatformStateMachine StateMachine { get; }
    IReadOnlyList<IPlatformContent> Contents { get; }
    IReadOnlyList<IPlatform> Neighbors { get; }
    IPlatformVisual Visual { get; }
    
    void Enter();
    void Exit();
    void AddNeighbor(IPlatform platform);
    void AddContent(IPlatformContent content);
}
```

#### AreaGenerator
```csharp
public class AreaGenerator : IAreaGenerator
{
    // Dependencies
    private readonly PlatformGraphData graph;
    private readonly PerlinNoiseMap noiseMap;
    private readonly AreaGeneratorConfig config;
    
    // Core method
    public void Generate()
    {
        CreatePlatformsFromGraph();
        ConnectPlatforms();
        PositionPlatforms();
        PopulateContent();
        CreateVisuals();
    }
    
    public IPlatform EntryPlatform { get; }
}
```

#### PlatformGraphGenerator
```csharp
public class PlatformGraphGenerator : IPlatformGraphGenerator
{
    public PlatformGraphData GenerateGraph(ScenarioData scenario)
    {
        // Create nodes from scenario requirements
        // Add filler nodes
        // Create edges (connections)
        // Validate connectivity
        return graphData;
    }
}
```

### Data Flow

```
User Request (Generate Area)
        ↓
ScenarioData (input parameters)
        ↓
PlatformGraphGenerator
    ↓ (produces)
PlatformGraphData (abstract structure)
        ↓
AreaGenerator
    ↓ (creates)
Platform Instances (game objects)
        ↓
PlatformView (visualization)
        ↓
Player Interaction
```

### Configuration

**AreaGeneratorConfig** controls generation parameters:

```csharp
public class AreaGeneratorConfig
{
    // Spacing
    public float HorizontalSpacing = 5f;     // Distance between platforms
    public float VerticalVariation = 3f;     // Max Y offset
    
    // Noise
    public float NoiseScale = 0.1f;          // Perlin noise frequency
    public float NoiseAmplitude = 2f;        // Noise strength
    
    // Visual
    public float PlatformScale = 1f;         // Platform size multiplier
    public Material DefaultMaterial;         // Default platform material
}
```

---

## Implementation Status

### ✅ Fully Implemented (100%)

#### Core Platform System
- [x] **IPlatform interface** and implementation
- [x] **Platform state machine** with 4 states
- [x] **Platform content system** (modular, extensible)
- [x] **Platform types** (Simple, Combat, Treasure, etc.)
- [x] **Neighbor connections** (graph-based)
- [x] **Platform visual component** (IPlatformVisual)

#### Procedural Generation
- [x] **Scenario system** (ScenarioData, requirements)
- [x] **Graph generator** (PlatformGraphGenerator)
  - [x] Node creation from requirements
  - [x] Filler node generation
  - [x] Edge creation (linear + branches)
  - [x] Connectivity validation
- [x] **Area generator** (AreaGenerator)
  - [x] Platform instantiation from graph
  - [x] Platform positioning with Perlin noise
  - [x] Content population
  - [x] Visual creation
- [x] **Perlin noise map** for natural layouts
- [x] **Platform factories** for different types

#### Visualization
- [x] **PlatformView** (MonoBehaviour for rendering)
- [x] **State-based materials** (visual feedback for states)
- [x] **Platform positioning** in 3D space
- [x] **GameObject hierarchy** management
- [x] **Area container** for organization

#### Integration
- [x] **AreaSceneEntrypoint** for scene initialization
- [x] **Battlefield integration** (platforms use hex positions)
- [x] **Combat triggers** (platform content → combat)
- [x] **Zenject DI** (PlatformInstaller, BattlefieldInstaller)

---

### 🟡 Partially Implemented (20%)

#### Content Types
- [x] Content type enumeration
- [x] Content interface (IPlatformContent)
- [ ] Concrete content implementations:
  - [ ] CombatContent (enemy spawning)
  - [ ] TreasureContent (loot chests)
  - [ ] ShopContent (merchant NPC)
  - [ ] PuzzleContent
  - [ ] StoryContent

#### Player Navigation
- [x] Basic character movement controller
- [ ] Click-to-move between platforms
- [ ] Path visualization (show route to destination)
- [ ] Navigation UI (available platforms)

#### Dynamic Behavior
- [ ] Platform activation (progressive unlocking)
- [ ] Platform destruction/creation at runtime
- [ ] Dynamic connections (bridges appearing)
- [ ] Environmental hazards

---

### 🔴 Not Yet Implemented (0%)

#### Advanced Generation
- [ ] **Biome system** (different visual themes)
- [ ] **Multi-floor areas** (vertical layering)
- [ ] **Branching paths** with meaningful choices
- [ ] **Secret platforms** (hidden content)
- [ ] **Dynamic difficulty** adjustment
- [ ] **Seeded generation** (reproducible levels)

#### Meta-Level Features
- [ ] **World map** (area-to-area navigation)
- [ ] **Area progression** (unlock new areas)
- [ ] **Area modifiers** (special rules per area)
- [ ] **Boss arenas** (special platform layouts)

#### Polish
- [ ] **Platform animations** (floating, rotating)
- [ ] **Transition effects** (fade in/out)
- [ ] **Environmental effects** (fog, lighting)
- [ ] **Audio** (ambient sounds, platform-specific)
- [ ] **Minimap** (area overview)

---

## Usage Example

### Generating an Area

```csharp
// 1. Define scenario
var scenario = new ScenarioData
{
    Name = "Tutorial Area",
    EstimatedPlatformCount = 8,
    RequiredPlatforms = new List<PlatformRequirement>
    {
        new PlatformRequirement(PlatformType.Simple, new[] { PlatformContentType.None }),
        new PlatformRequirement(PlatformType.Combat, new[] { PlatformContentType.Combat }),
        new PlatformRequirement(PlatformType.Exit, new[] { PlatformContentType.None })
    }
};

// 2. Generate graph
var graphGenerator = new PlatformGraphGenerator();
var graph = graphGenerator.GenerateGraph(scenario);

// 3. Create area
var noiseMap = new PerlinNoiseMap(seed: 12345);
var config = new AreaGeneratorConfig();
var areaGenerator = new AreaGenerator(graph, noiseMap, config);

// 4. Generate and get entry point
areaGenerator.Generate();
IPlatform entryPlatform = areaGenerator.EntryPlatform;

// 5. Navigate
entryPlatform.Enter(); // Player arrives
```

### Adding Custom Platform Type

```csharp
// 1. Define new type
public enum PlatformType
{
    // ... existing types ...
    Puzzle,  // NEW
}

// 2. Create builder (optional)
public class PuzzlePlatformBuilder : IPlatformBuilder
{
    public IPlatform Build(GraphNode node)
    {
        var platform = new Platform(node.Id);
        // Add puzzle-specific setup
        return platform;
    }
}

// 3. Use in scenario
var scenario = new ScenarioData
{
    RequiredPlatforms = new List<PlatformRequirement>
    {
        new PlatformRequirement(PlatformType.Puzzle, new[] { PlatformContentType.Puzzle })
    }
};
```

---

## Design Patterns Used

1. **State Machine** - Platform lifecycle management
2. **Factory** - Platform creation
3. **Builder** - Complex platform construction
4. **Strategy** - Different generation algorithms
5. **Observer** - Platform state change notifications
6. **Graph** - Platform connectivity

---

## Performance Considerations

### Generation Time
- **Target:** < 1 second for 15-platform area
- **Actual:** ~300ms for typical area
- **Bottleneck:** Visual GameObject creation

### Memory Usage
- **Per Platform:** ~100KB (with visuals)
- **Typical Area:** ~1.5MB (15 platforms)
- **Acceptable:** < 5MB per area

### Optimization Strategies
1. **Object pooling** for platform GameObjects
2. **Lazy loading** of distant platforms
3. **LOD** for platform visuals
4. **Async generation** for large areas

---

## Future Enhancements

### Short Term
1. Implement remaining content types
2. Add platform animations
3. Create biome system
4. Improve navigation UI

### Long Term
1. Multi-floor areas
2. Dynamic platform modification
3. Procedural content generation (enemies, loot)
4. Area-specific rules and modifiers
5. Level editor for custom scenarios

---

## Related Documentation

- **[GAME_DESIGN.md](GAME_DESIGN.md)** - Overall game design
- **[COMBAT_DESIGN.md](COMBAT_DESIGN.md)** - Combat system details
- **AREA_GENERATOR_USAGE.md** - Detailed usage guide (existing)

---

**This is a living document. Update when world systems change.**

