# Zenject Multiple Constructor Fix

## Issue

When `CombatConfig` had two constructors added (parameterized and parameterless), Zenject continued to throw:

```
ZenjectException: Passed unnecessary parameters when injecting into type 'CombatConfig'. 
Extra Parameters: float, HexOrientation
```

Even though the parameterized constructor existed and matched the `.WithArguments()` parameters.

## Root Cause

**Zenject behavior with multiple constructors:**
- When a class has multiple constructors, Zenject doesn't automatically know which one to use
- Without explicit guidance, Zenject defaults to the **parameterless constructor**
- This caused the `.WithArguments(2f, HexOrientation.Flat)` parameters to be unused and "unnecessary"

## Solution

Added the `[Inject]` attribute to the parameterized constructor:

```csharp
[Inject]
public CombatConfig(float hexCellSize, HexOrientation hexOrientation)
{
    HexCellSize = hexCellSize;
    HexOrientation = hexOrientation;
}
```

This explicitly tells Zenject: **"Use THIS constructor for dependency injection."**

## Zenject Best Practice

**Rule:** When you have multiple constructors, always mark the one intended for dependency injection with `[Inject]`.

### Examples

❌ **Bad - Ambiguous:**
```csharp
public class MyConfig
{
    public MyConfig(int value) { }
    public MyConfig() { }
}
// Zenject doesn't know which to use!
```

✅ **Good - Explicit:**
```csharp
public class MyConfig
{
    [Inject]
    public MyConfig(int value) { }
    
    public MyConfig() { }
}
// Zenject knows to use the [Inject] constructor
```

## Changes Made

**File:** `Scripts/Combat/Config/CombatConfig.cs`

1. Added `using Zenject;` directive
2. Added `[Inject]` attribute to parameterized constructor
3. Updated XML documentation to clarify this constructor is for DI

## Why Not Remove the Parameterless Constructor?

We kept both constructors because:
- ✅ **Backward compatibility** - Other code might create `CombatConfig` directly
- ✅ **Flexibility** - Can create instances with defaults outside of DI
- ✅ **Testing** - Easier to create test instances
- ✅ **Clear intent** - `[Inject]` makes DI intent explicit

## Verification

The binding in `CombatInstaller.cs` now works correctly:

```csharp
Container.Bind<CombatConfig>().AsSingle().WithArguments(
    2f,  // hexCellSize
    HexOrientation.Flat  // hexOrientation
);
```

Zenject will:
1. See the `[Inject]` attribute on the parameterized constructor
2. Use that constructor: `new CombatConfig(2f, HexOrientation.Flat)`
3. Inject the configured instance into `CombatController` and `CombatControllerFactory`

## Related Documentation

- See `COMBATCONFIG_FIX_SUMMARY.md` for the full constructor addition details
- See `ZENJECT_FIX_SUMMARY.md` for all Zenject configuration fixes

