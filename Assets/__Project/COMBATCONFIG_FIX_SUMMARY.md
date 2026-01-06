# CombatConfig Constructor Fix Summary

## Issue

**ZenjectException:** "Passed unnecessary parameters when injecting into type 'CombatConfig'. Extra Parameters: float, HexOrientation"

## Root Cause

The [`CombatInstaller.cs`](Scripts/Combat/DI/CombatInstaller.cs) was attempting to bind `CombatConfig` with constructor arguments:

```csharp
Container.Bind<CombatConfig>().AsSingle().WithArguments(
    2f,  // hexCellSize - default value
    HexOrientation.Flat  // hexOrientation - default value
);
```

However, the original [`CombatConfig.cs`](Scripts/Combat/Config/CombatConfig.cs) class had no constructor that accepted these parameters. It only had auto-properties with default values:

```csharp
public class CombatConfig
{
    public float HexCellSize { get; set; } = 2f;
    public HexOrientation HexOrientation { get; set; } = HexOrientation.Flat;
}
```

When Zenject tried to instantiate `CombatConfig` with the provided arguments `(float, HexOrientation)`, it failed because there was no matching constructor signature.

## Solution

Added explicit constructors to `CombatConfig` to accept the configuration parameters, with the `[Inject]` attribute to tell Zenject which constructor to use:

```csharp
public class CombatConfig
{
    public float HexCellSize { get; }
    public HexOrientation HexOrientation { get; }
    
    // Constructor with parameters for Zenject injection
    [Inject]
    public CombatConfig(float hexCellSize, HexOrientation hexOrientation)
    {
        HexCellSize = hexCellSize;
        HexOrientation = hexOrientation;
    }
    
    // Parameterless constructor for backward compatibility
    public CombatConfig() : this(2f, HexOrientation.Flat)
    {
    }
}
```

**Important:** The `[Inject]` attribute is required when a class has multiple constructors. Without it, Zenject defaults to the parameterless constructor, causing the `.WithArguments()` parameters to be treated as "unnecessary."

### Changes Made

1. **Added parameterized constructor** - Accepts `float hexCellSize` and `HexOrientation hexOrientation` parameters
2. **Added [Inject] attribute** - Explicitly marks the parameterized constructor for Zenject to use when multiple constructors exist
3. **Changed properties to read-only** - Properties now use `get` only (immutable configuration)
4. **Added parameterless constructor** - Delegates to parameterized constructor with default values for backward compatibility
5. **Added XML documentation** - Documented both constructors
6. **Added using Zenject** - Required for the [Inject] attribute

## Benefits

✅ **Explicit Configuration** - Configuration values are now explicitly passed during construction  
✅ **Immutability** - Configuration cannot be modified after creation, preventing bugs  
✅ **Flexibility** - Different scenes/contexts can use different configurations  
✅ **SOLID Principles** - Follows dependency injection best practices  
✅ **Backward Compatible** - Parameterless constructor maintains compatibility with existing code  

## Files Modified

- **Scripts/Combat/Config/CombatConfig.cs** - Added constructors and made properties immutable

## Verification

The Zenject binding in `CombatInstaller.cs` now works correctly:
- Zenject calls `new CombatConfig(2f, HexOrientation.Flat)`
- The parameterized constructor accepts the arguments
- Configuration is properly injected into `CombatController` and `CombatControllerFactory`

## Related Fixes

This fix is part of a series of Zenject configuration fixes:
1. ✅ Fixed missing BattlefieldInstaller reference → CombatInstaller
2. ✅ Fixed IBattlefield factory binding (added `.To<Battlefield>()`)
3. ✅ Fixed PlatformInstaller architecture issues
4. ✅ Fixed CombatConfig constructor parameters (this fix)

All Zenject dependency injection issues should now be resolved, and the scene should load without exceptions.

