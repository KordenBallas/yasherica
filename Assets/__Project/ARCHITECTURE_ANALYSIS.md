# Architecture Design Issues Analysis

## Critical Issues

### 1. **Builder Pattern Inconsistency**
**Problem:** `PlatformGraphBuilder` stores `IPlatformBuilder` instances, but `IPlatformBuilder.Build()` returns `IPlatform` (a fully constructed object), not graph node data. The builder is creating platform instances when it should only be building graph structure (data).

**Issue:**
```csharp
// PlatformGraphBuilder stores builders
private List<IPlatformBuilder> platformBuilders = new();

// But IPlatformBuilder.Build() returns IPlatform
IPlatform Build();  // This creates a full platform instance!

// How does PlatformGraphBuilder convert IPlatform to GraphNode?
public PlatformGraphData Build() { ... }  // What goes here?
```

**Impact:** 
- Builders create platform instances prematurely (before placement)
- Graph should be data-only, not contain live objects
- Unclear how to extract graph data from platform instances

**Solution:**
- `IPlatformBuilder` should build `GraphNode` or `PlatformDefinition` (data), not `IPlatform`
- Or have separate builders: `PlatformDefinitionBuilder` for graph, `PlatformBuilder` for instances

---

### 2. **Model/Visual Separation Contradiction**
**Problem:** Decorations are placed in the Model section, but they're purely visual elements (stones, trees). This contradicts the stated separation principle.

**Issue:**
```csharp
// IPlatform - Model section
List<PlatformDecoration> Decorations { get; }  // Visual data in model?

// But Visual section has:
IPlatformVisual Visual { get; }  // Position, Size, TopBoundary
```

**Impact:**
- Unclear ownership: Are decorations part of model or visual?
- Decorations have Position/Rotation/Scale (visual) but stored in model
- Violates the stated separation principle

**Solution:**
- Move `Decorations` to `IPlatformVisual` OR
- Create separate `IPlatformDecorations` interface
- Clarify: Decorations are visual metadata, not model logic

---

### 3. **IBattlefield Encapsulation Violation**
**Problem:** `IBattlefield` exposes `IHexGrid Grid { get; }` directly, breaking encapsulation. The whole point of `IBattlefield` was to encapsulate the grid.

**Issue:**
```csharp
public interface IBattlefield
{
    IHexGrid Grid { get; }  // Direct exposure breaks encapsulation!
    // ...
}
```

**Impact:**
- Clients can directly manipulate the grid, bypassing Battlefield logic
- No way to enforce invariants (e.g., grid must be initialized before use)
- Defeats the purpose of encapsulation

**Solution:**
- Remove `Grid` property from interface
- Add methods like `GetCellAt(HexCoordinates)`, `GetCellsInRange()`, etc.
- Only expose grid functionality through Battlefield's interface

---

### 4. **Platform Content Single vs Multiple**
**Problem:** `IPlatform` has `IPlatformContent Content { get; set; }` (singular), but the builder example shows multiple content types on one platform.

**Issue:**
```csharp
// Interface says single content
IPlatformContent Content { get; set; }

// But builder example shows multiple:
.WithContent(new NpcContent(npcId))
.WithContent(new QuestContent(questId))  // Multiple contents?
```

**Impact:**
- Can't have NPC + Quest + Loot on same platform
- Unclear how multiple content types are handled
- Builder pattern suggests multiple, interface suggests single

**Solution:**
- Change to `List<IPlatformContent> Contents { get; }` OR
- Create composite content type `CompositePlatformContent`
- Clarify the design intent

---

### 5. **State Machine Circular Dependency**
**Problem:** `PlatformStateMachine` holds reference to `IPlatform`, and states receive `IPlatform` in their methods, creating tight coupling.

**Issue:**
```csharp
public class PlatformStateMachine
{
    private IPlatform owner;  // Holds platform
    
    public void ChangeState(IPlatformState newState)
    {
        currentState?.OnExit(owner);  // Passes platform to state
    }
}

public interface IPlatformState
{
    void OnEnter(IPlatform platform);  // Receives platform
    void OnUpdate(IPlatform platform);
}
```

**Impact:**
- States need full `IPlatform` access, making them tightly coupled
- Hard to test states in isolation
- States might access implementation details

**Solution:**
- Pass only needed data to states (context object)
- Or use events/signals instead of direct platform reference
- Consider Command pattern for state actions

---

### 6. **IPlatformVisual Ownership and Lifecycle**
**Problem:** `IPlatformVisual` has setters and `UpdateVisual()` method, but unclear who owns it, when it's created, and who calls `UpdateVisual()`.

**Issue:**
```csharp
public interface IPlatformVisual
{
    Vector3 Position { get; set; }  // Who sets this?
    Vector2 Size { get; set; }
    List<Vector3> TopBoundary { get; set; }
    
    void UpdateVisual();  // Who calls this? When?
}
```

**Impact:**
- Unclear ownership: Is it owned by Platform or View?
- When is visual data updated?
- Who is responsible for calling `UpdateVisual()`?

**Solution:**
- Make `IPlatformVisual` immutable or use builder pattern
- Clarify ownership: Platform owns visual data, View reads it
- Remove `UpdateVisual()` or make it internal
- Consider making it a value object (struct/immutable class)

---

### 7. **AreaGenerator Missing Input**
**Problem:** `IAreaGenerator.Generate(GameContext context)` only takes `GameContext`, but needs `PlatformGraphData` to build the area. Unclear where graph comes from.

**Issue:**
```csharp
public interface IAreaGenerator
{
    void Generate(GameContext context);  // Where's PlatformGraphData?
    AreaGenerationResult Result { get; }
}
```

**Impact:**
- Unclear workflow: Does AreaGenerator generate graph internally?
- Or should it accept `PlatformGraphData` as parameter?
- Builder example shows separate graph building, but interface doesn't support it

**Solution:**
- Add overload: `Generate(GameContext context, PlatformGraphData graph)`
- Or make it clear that AreaGenerator orchestrates all phases internally
- Clarify the intended workflow

---

### 8. **PlatformFactory Violates Open/Closed Principle**
**Problem:** `PlatformFactory` uses switch statement on `PlatformType`, making it hard to extend with new platform types.

**Issue:**
```csharp
public IPlatform Create(PlatformType type)
{
    switch (type)  // Must modify this for new types
    {
        case PlatformType.Simple:
            return container.Resolve<SimplePlatform.Factory>().Create();
        case PlatformType.Combat:
            return container.Resolve<CombatPlatform.Factory>().Create();
        // Must add new cases here
    }
}
```

**Impact:**
- Adding new platform types requires modifying factory
- Violates Open/Closed Principle
- Not extensible

**Solution:**
- Use factory registry pattern
- Or use Zenject's `IFactory<T>` with type resolution
- Or use Strategy pattern with factory map

---

### 9. **CombatPlatform Battlefield Lifecycle**
**Problem:** `CombatPlatform` has `IBattlefield Battlefield` attribute, but unclear when it's created, initialized, and activated.

**Issue:**
```csharp
// CombatPlatform has battlefield
IBattlefield Battlefield { get; }  // When is this created?

// But when does it get initialized?
// When does it activate?
// Who owns the battlefield lifecycle?
```

**Impact:**
- Unclear initialization order
- Battlefield might be null when accessed
- No clear activation/deactivation flow

**Solution:**
- Define initialization in `CombatPlatform.Initialize()`
- Add lifecycle methods: `CreateBattlefield()`, `ActivateBattlefield()`
- Or use lazy initialization with null checks
- Document the lifecycle

---

### 10. **Mutable Collections in Interface**
**Problem:** `IPlatform` exposes mutable collections (`List<IPlatform> Neighbors`, `List<PlatformDecoration> Decorations`), allowing external modification.

**Issue:**
```csharp
public interface IPlatform
{
    List<IPlatform> Neighbors { get; }  // Mutable!
    List<PlatformDecoration> Decorations { get; }  // Mutable!
}
```

**Impact:**
- External code can modify internal state
- Breaks encapsulation
- Hard to maintain invariants

**Solution:**
- Use `IReadOnlyList<T>` for getters
- Keep mutable lists private
- Provide controlled modification methods: `AddNeighbor()`, `RemoveNeighbor()`

---

## Moderate Issues

### 11. **IBattlefield IsActive Redundancy**
**Problem:** `IBattlefield` has both `IsActive` property and `Activate()`/`Deactivate()` methods, creating redundancy.

**Issue:**
```csharp
bool IsActive { get; set; }  // Property
void Activate();
void Deactivate();
```

**Impact:**
- Can set `IsActive` directly, bypassing `Activate()`/`Deactivate()`
- Unclear which to use
- Potential for inconsistent state

**Solution:**
- Make `IsActive` read-only
- Or remove `Activate()`/`Deactivate()` if property is sufficient
- Add validation in setter if keeping both

---

### 12. **IPlatformPlacementGenerator Mutable Property**
**Problem:** `IPlatformPlacementGenerator` has mutable `GeneratedPlatforms` property, but `PlacePlatforms()` is void. Unclear when platforms are available.

**Issue:**
```csharp
void PlacePlatforms(PlatformGraphData graph, PerlinNoiseMap noiseMap);
List<IPlatform> GeneratedPlatforms { get; }  // When is this populated?
```

**Impact:**
- Unclear if platforms are available immediately after `PlacePlatforms()`
- Property might be empty before call
- No clear contract

**Solution:**
- Return `List<IPlatform>` from `PlacePlatforms()`
- Or make it clear that property is populated after method call
- Consider using result object pattern

---

### 13. **GraphNode Contains PlatformType**
**Problem:** `GraphNode` has `PlatformType Type`, but with generic `Platform` class, type might be determined by content instead.

**Issue:**
```csharp
public class GraphNode
{
    public PlatformType Type;  // SimplePlatform vs CombatPlatform?
    public PlatformContentType ContentType;  // NPC, Enemy, etc.
    // ...
}
```

**Impact:**
- Redundancy: Type might be derivable from ContentType
- Unclear when to use SimplePlatform vs CombatPlatform
- Type might not be needed if Platform is generic

**Solution:**
- Clarify: Is `PlatformType` needed, or can it be derived?
- Or make it explicit: CombatPlatform is determined by having Battlefield content
- Consider removing if redundant

---

### 14. **Missing Error Handling**
**Problem:** No error handling or validation mentioned in interfaces. What happens when:
- Platform initialized with null visual?
- Battlefield initialized with empty boundary?
- State transition is invalid?

**Impact:**
- Unclear failure modes
- No contract for error handling
- Potential runtime exceptions

**Solution:**
- Add validation methods
- Define exception types
- Use Result pattern for operations that can fail
- Document preconditions/postconditions

---

### 15. **No Clear Platform Registry**
**Problem:** Old code had `PlatformGraphRegistry`, but new architecture doesn't mention how platforms are tracked/queried.

**Issue:**
- How do you find a platform by ID?
- How do you get all platforms?
- How does character navigation work without registry?

**Impact:**
- Unclear how to query platform graph at runtime
- Character movement might need platform lookup
- No central place to manage platform collection

**Solution:**
- Add `IPlatformRegistry` or `IPlatformGraph` interface
- Or make `AreaGenerator.Result` contain registry
- Document platform lookup strategy

---

## Minor Issues / Suggestions

### 16. **IPlatformVisual UpdateVisual() Unclear**
- Method name suggests it updates something, but what?
- Should it be `Refresh()` or `Rebuild()`?
- Or should it be removed if visual is just data?

### 17. **HexCoordinates as Interface**
- `IHexCoordinates` is mentioned but might be over-engineering
- Consider if it should be a struct/value type instead

### 18. **ContentType vs PlatformContent**
- `IPlatformContent` has `ContentType Type` property
- But also separate content classes (NpcContent, EnemyContent)
- Consider if type enum is needed or can use `GetType()`

### 19. **State Machine Update()**
- `PlatformStateMachine.Update()` is called, but when?
- Should platforms have Update loop?
- Or is it event-driven?

### 20. **Builder Static Method**
- `PlatformBuilder.NewInstance()` suggests static factory
- But with DI, should use factory injection instead
- Consider constructor or factory injection

---

## Summary of Critical Issues

1. **Builder creates instances instead of graph data** - Major architectural issue
2. **Decorations in model but are visual** - Contradicts separation principle
3. **IBattlefield exposes grid directly** - Breaks encapsulation
4. **Single content vs multiple content** - Interface/builder mismatch
5. **State machine circular dependency** - Tight coupling
6. **Visual ownership unclear** - Lifecycle issues
7. **AreaGenerator missing input** - Unclear workflow
8. **PlatformFactory violates OCP** - Not extensible
9. **CombatPlatform battlefield lifecycle** - Unclear initialization
10. **Mutable collections in interface** - Breaks encapsulation

These issues should be addressed before implementation to avoid refactoring later.

