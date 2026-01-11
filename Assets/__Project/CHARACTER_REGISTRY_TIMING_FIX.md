# Character Registry Timing Fix - Detailed Analysis

## Problem

When entering a CombatPlatform, the system was throwing errors:
```
[CharacterRegistry] No player character registered
[CharacterCombatInitializer] No player character found in registry
```

## Root Cause

The character GameObject is a **scene object referenced by AreaSceneEntrypoint**, but Zenject doesn't automatically inject dependencies into referenced scene objects. Only objects that are:
1. Created by Zenject (factories, FromNewComponentOnNewGameObject)
2. Explicitly bound with FromComponentInHierarchy

...receive automatic injection.

### Failed Attempt #1

Initial fix attempted to make `CharacterMovementController` self-register using `[Inject]`:

```csharp
public class CharacterMovementController : MonoBehaviour
{
    [Inject] private ICharacterRegistry _characterRegistry;  // ❌ Never injected!
    
    void Start()
    {
        _characterRegistry.RegisterCharacter(transform);  // ❌ NullReferenceException!
    }
}
```

**Why it failed:** The character is just a Transform reference in AreaSceneEntrypoint's serialized field. Zenject never knew about this GameObject and never injected into it.

## Solution

**Manual Dependency Injection** - Explicitly inject into the character controller from AreaSceneEntrypoint.

### Changes Made

#### 1. CharacterMovementController.cs

Added self-registration logic (kept from first attempt):

```csharp
using Zenject;

public class CharacterMovementController : MonoBehaviour
{
    [Inject] private ICharacterRegistry _characterRegistry;
    
    void Start()
    {
        // Register with registry first (before any movement logic)
        if (_characterRegistry != null)
        {
            _characterRegistry.RegisterCharacter(transform);
            Debug.Log("[CharacterMovementController] Self-registered with CharacterRegistry");
        }
        else
        {
            Debug.LogWarning("[CharacterMovementController] CharacterRegistry not injected - character will not be available for combat");
        }
        
        // ... rest of Start() logic
    }
    
    void OnDestroy()
    {
        // Unregister when destroyed
        if (_characterRegistry != null)
        {
            _characterRegistry.UnregisterCharacter(transform);
            Debug.Log("[CharacterMovementController] Unregistered from CharacterRegistry");
        }
    }
}
```

#### 2. AreaSceneEntrypoint.cs

Added **manual injection** after positioning the character:

```csharp
[Inject] private DiContainer _container;

public void GenerateArea()
{
    // ... area generation logic ...
    
    // Place character at entry platform
    if (areaGenerator.EntryPlatform != null && characterTransform != null)
    {
        currentPlatform = areaGenerator.EntryPlatform;
        characterTransform.position = currentPlatform.Visual.Position + Vector3.up * 2f;
        
        // Get character controller
        var characterController = characterTransform.GetComponent<Character.CharacterMovementController>();
        if (characterController != null)
        {
            // ✅ Manually inject dependencies into scene character object
            _container.Inject(characterController);
            Debug.Log("[AreaSceneEntrypoint] Injected dependencies into CharacterMovementController");
        }
    }
}
```

## Execution Flow - Fixed

```
Scene loads
  └─> Unity Awake() phase
      └─> CharacterMovementController.Awake()
      └─> AreaSceneEntrypoint.Awake()
      
  └─> Zenject initialization phase
      └─> Install all installers
      └─> Inject all dependencies
          └─> Inject into AreaSceneEntrypoint (IInitializable)
      └─> Call IInitializable.Initialize()
          └─> AreaSceneEntrypoint.Initialize()
              └─> GenerateArea()
                  └─> Position character
                  └─> _container.Inject(characterController)  ✅
                      └─> Injects ICharacterRegistry into character
      
  └─> Unity Start() phase
      └─> CharacterMovementController.Start()
          └─> _characterRegistry.RegisterCharacter(transform)  ✅
          └─> Character is now registered!
      
  └─> Unity Update() phase
      └─> CharacterMovementController.Update()
          └─> HandleDash() → Enter CombatPlatform
              └─> CharacterCombatInitializer tries to get player
                  └─> _characterRegistry.GetPlayerCharacter()  ✅ Found!
```

## Why This Works

1. **Manual Injection Timing**: `_container.Inject()` is called during `AreaSceneEntrypoint.Initialize()`, which happens in Zenject's initialization phase (after Awake, before Start)

2. **Start() Guarantee**: When Unity calls `CharacterMovementController.Start()`, the injection has already completed, so `_characterRegistry` is valid

3. **Registration Before Update**: Character registers itself in `Start()`, which is guaranteed to run before `Update()` where dash/combat entry can be triggered

4. **Architecture Benefits**:
   - ✅ Character owns its registration lifecycle (self-registers/unregisters)
   - ✅ Follows SOLID principles (Single Responsibility)
   - ✅ Follows DIP (depends on ICharacterRegistry interface)
   - ✅ Explicit about dependencies (uses Zenject)
   - ✅ Testable (can mock ICharacterRegistry)

## Key Lesson

When using Zenject with scene objects that are **referenced but not bound**:
- ❌ Field injection with `[Inject]` alone won't work
- ✅ Explicitly call `_container.Inject(component)` to manually inject dependencies

This pattern is already used in the codebase (e.g., `CharacterCombatInitializer` line 98) for dynamically added components.

## Testing

To verify the fix works:
1. Enter play mode
2. Check console for: `[CharacterMovementController] Self-registered with CharacterRegistry`
3. Dash into a CombatPlatform
4. Should NOT see the registration errors anymore
5. Combat initialization should proceed successfully


