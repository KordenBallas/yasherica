# Zenject Injection Timing Fix Summary

## Issue

**NullReferenceException** at `CombatPlatform.Initialize()` line 31:
```
_controller = _controllerFactory.Create();  // _controllerFactory is null
```

## Root Cause

### The Problem: Injection Timing

The `AreaSceneEntrypoint` MonoBehaviour had a `[Inject]` field:

```csharp
[Inject]
private IFactory<ICombatController> _controllerFactory;

void Start()
{
    GenerateArea();  // Uses _controllerFactory - but it's still null!
}
```

### Unity + Zenject Lifecycle

MonoBehaviours in Unity scenes follow this lifecycle:

1. **Awake()** - Unity initialization
2. **Zenject injection** - Happens after all Awake() calls complete
3. **Start()** - Game logic begins

**The bug:** `Start()` was calling `GenerateArea()` which used `_controllerFactory`, but Zenject hadn't injected it yet!

### Dependency Chain Leading to Crash

```
AreaSceneEntrypoint.Start()
  └─> GenerateArea()
      └─> new AreaGenerator(..., _controllerFactory)  // null!
          └─> CreatePlatformFromNode()
              └─> new CombatPlatform(..., _controllerFactory)  // null!
                  └─> Initialize()
                      └─> _controllerFactory.Create()  // NullReferenceException!
```

## Solution

Implemented the **IInitializable** interface pattern, which is Zenject's recommended approach for post-injection initialization.

### Changes Made

#### 1. AreaSceneEntrypoint.cs

**Before:**
```csharp
public class AreaSceneEntrypoint : MonoBehaviour
{
    [Inject]
    private IFactory<ICombatController> _controllerFactory;
    
    void Start()
    {
        GenerateArea();
    }
}
```

**After:**
```csharp
public class AreaSceneEntrypoint : MonoBehaviour, IInitializable
{
    [Inject]
    private IFactory<ICombatController> _controllerFactory;
    
    // Called by Zenject AFTER injection is complete
    public void Initialize()
    {
        GenerateArea();
    }
    
    // Start() method removed
}
```

#### 2. GameInstaller.cs

Added binding to register `AreaSceneEntrypoint` with Zenject:

```csharp
// Scene entrypoints - bind to IInitializable so Zenject calls Initialize() after injection
Container.BindInterfacesTo<AreaSceneEntrypoint>().FromComponentInHierarchy().AsSingle();
```

This binding tells Zenject to:
1. Find the `AreaSceneEntrypoint` component in the scene hierarchy
2. Bind it to its implemented interfaces (`IInitializable`)
3. Call `Initialize()` after all dependencies are injected

## How It Works Now

### New Lifecycle with IInitializable

1. **Awake()** - Unity initialization
2. **Zenject injection** - Injects `_controllerFactory` into `AreaSceneEntrypoint`
3. **Zenject calls Initialize()** - `_controllerFactory` is now valid!
4. **Start()** - (not used anymore)

### Execution Flow

```
Scene loads
  └─> Zenject SceneContext.Awake()
      └─> Install all installers
          └─> GameInstaller.InstallBindings()
              └─> Binds AreaSceneEntrypoint as IInitializable
      └─> Inject all dependencies
          └─> Inject _controllerFactory into AreaSceneEntrypoint
      └─> Call all IInitializable.Initialize()
          └─> AreaSceneEntrypoint.Initialize()
              └─> GenerateArea()
                  └─> _controllerFactory is valid ✅
```

## Benefits of IInitializable Pattern

✅ **Guaranteed Injection** - Initialize() is called AFTER all dependencies are injected  
✅ **Zenject Best Practice** - Official recommended pattern for MonoBehaviour initialization  
✅ **No Timing Hacks** - No coroutines, no null checks, no frame delays  
✅ **Explicit Dependencies** - Clear that this component requires DI  
✅ **Consistent** - Same pattern used in other Zenject-managed components  

## Alternative Approaches Considered

### Option 1: Coroutine Delay (Rejected)
```csharp
void Start()
{
    StartCoroutine(GenerateAreaDelayed());
}

IEnumerator GenerateAreaDelayed()
{
    yield return null;  // Wait one frame
    GenerateArea();
}
```
❌ Fragile timing hack  
❌ Not guaranteed to work  
❌ Adds unnecessary complexity  

### Option 2: Manual Context Menu Only (Rejected)
```csharp
// Remove Start() entirely, manual generation only
[ContextMenu("Generate Area")]
public void GenerateArea() { ... }
```
❌ Loses automatic generation on scene start  
❌ Requires manual intervention every time  

### Option 3: IInitializable (Implemented) ✅
See solution above.

## Files Modified

1. **Scripts/Area/AreaSceneEntrypoint.cs**
   - Implemented `IInitializable` interface
   - Renamed `Start()` to `Initialize()`
   - Added XML documentation

2. **Scripts/Core/DI/GameInstaller.cs**
   - Added `Container.BindInterfacesTo<AreaSceneEntrypoint>()` binding
   - Added explanatory comment

## Verification

The fix ensures:
- ✅ `_controllerFactory` is injected before use
- ✅ No NullReferenceException in `CombatPlatform.Initialize()`
- ✅ Area generation happens automatically on scene start
- ✅ `[ContextMenu("Generate Area")]` still works for manual regeneration

## Related Fixes

This is part of a series of Zenject configuration fixes:
1. ✅ Fixed missing BattlefieldInstaller → CombatInstaller reference
2. ✅ Fixed IBattlefield factory binding
3. ✅ Fixed PlatformInstaller architecture issues
4. ✅ Fixed CombatConfig constructor parameters
5. ✅ Added [Inject] attribute for multiple constructors
6. ✅ Fixed injection timing with IInitializable (this fix)

All Zenject issues should now be resolved!

