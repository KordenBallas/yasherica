# Combat Camera Setup Guide

This guide provides step-by-step instructions for setting up the combat camera system in Unity scenes.

## Overview

The combat camera system uses two Cinemachine 3.x cameras:
1. **IsometricCamera** - Default exploration camera (X:30, Y:45, Z:0)
2. **CombatCamera** - Combat-specific camera (X:45, Y:90, Z:0)

The `CameraService` manages smooth transitions between these cameras when entering/exiting combat.

## Prerequisites

- **Cinemachine 3.x** package installed (uses `Unity.Cinemachine` namespace)
- CameraService script compiled
- CameraConfig ScriptableObject created

**Note**: This implementation uses Cinemachine 3.x API (`CinemachineCamera`). If you're using Cinemachine 2.x, upgrade to 3.x via Package Manager.

## Scene Setup Steps

### 1. Create CameraConfig Asset

1. In Unity Project window, navigate to `Assets/__Project/Resources/`
2. Right-click → Create → Config → Camera Config
3. Name it `CameraConfig`
4. Configure parameters in Inspector:
   - **Orthographic Size**: 8 (recommended for combat view)
   - **Damping X/Y/Z**: 0.5 (tighter following for combat)
   - **Transition Time**: 2 (seconds for smooth camera rotation)
   - **Isometric Rotation**: X:30, Y:45, Z:0
   - **Combat Rotation**: X:45, Y:90, Z:0

### 2. Setup IsometricCamera

The IsometricCamera should already exist in your scene. Verify its settings:

1. Select `IsometricCamera` in Hierarchy
2. In **CinemachineCamera** component (Cinemachine 3.x):
   - **Priority**: 10 (active by default)
   - **Lens → Orthographic Size**: 10
   - **Rotation**: X:30, Y:45, Z:0
3. Ensure it has a Tracking Target assigned

### 3. Create CombatCamera

1. In Hierarchy, duplicate `IsometricCamera`
2. Rename the duplicate to `CombatCamera`
3. Configure **CinemachineCamera** component (Cinemachine 3.x):
   - **Priority**: 0 (inactive by default)
   - **Lens → Orthographic Size**: 8 (will be set by CameraConfig)
   - **Rotation**: X:45, Y:90, Z:0 (will be interpolated by CameraService)
4. Keep the same Tracking Target as IsometricCamera
5. If using CinemachinePositionComposer:
   - Damping values will be applied by CameraService from CameraConfig

### 4. Create CameraService GameObject

1. In Hierarchy, create empty GameObject: Right-click → Create Empty
2. Name it `CameraService`
3. Add Component: `CameraService` script
4. In Inspector, assign references:
   - **Isometric Camera**: Drag IsometricCamera from Hierarchy
   - **Combat Camera**: Drag CombatCamera from Hierarchy
   - **Config**: Drag CameraConfig from Resources folder
5. Position doesn't matter (service only manages references)

### 5. Verify Scene Context

Ensure your scene has a SceneContext with appropriate installers:

1. Select `SceneContext` in Hierarchy
2. Verify that `GameInstaller` is in the Mono Installers list
3. The GameInstaller automatically binds:
   - `ICameraService` → `CameraService` (FromComponentInHierarchy)
   - `CameraConfig` → from Resources

## Testing

### In Play Mode

1. Enter Play Mode
2. Move your character to a CombatPlatform
3. Observe:
   - Camera should smoothly transition from X:30,Y:45 to X:45,Y:90
   - Cinemachine Brain handles position blending
   - Rotation interpolates smoothly over 2 seconds (or configured time)
4. Exit the platform
5. Camera should smoothly transition back to isometric view

### Debug Verification

Check Console for these messages:
- `[CameraService] Switching to combat camera (rotation: (45, 90, 0), time: 2s)`
- `[CombatActiveState] Entering combat active state for platform X`
- `[CameraService] Camera rotation transition completed to (45, 90, 0)`

## Troubleshooting

### Camera doesn't switch

**Problem**: Camera stays in isometric view when entering combat.

**Solutions**:
- Verify `CameraService` GameObject exists in scene
- Check that both cameras are assigned in CameraService Inspector
- Ensure GameInstaller is on SceneContext
- Check Console for errors about missing ICameraService

### Camera switches but doesn't rotate

**Problem**: Camera priority changes but rotation stays the same.

**Solutions**:
- Verify CameraConfig is assigned to CameraService
- Check that rotation values are different in CameraConfig
- Ensure CombatCamera initial rotation is set correctly

### Jerky/instant transition

**Problem**: Camera jumps instead of smooth transition.

**Solutions**:
- Increase Transition Time in CameraConfig (try 2-3 seconds)
- Check Main Camera has CinemachineBrain component
- Verify Default Blend Time in CinemachineBrain (should be 1-2 seconds)

### CameraService not found by Zenject

**Problem**: Injection fails with "Unable to resolve ICameraService".

**Solutions**:
- Ensure CameraService component is attached to GameObject in scene
- Verify GameInstaller has `Container.Bind<ICameraService>().To<CameraService>().FromComponentInHierarchy()...`
- Check that GameObject with CameraService is active in Hierarchy

## Architecture Notes

### Dependency Flow

```
CombatPlatform (requires ICombatController and ICameraService)
    ↓
CombatActiveState (receives both via constructor)
    ↓
CameraService (manages Cinemachine cameras)
    ↓
Cinemachine Virtual Cameras (in scene)
```

### State Transitions

1. Player enters CombatPlatform
2. `CombatPlatform.Enter()` → transitions to `CombatActiveState`
3. `CombatActiveState.OnEnter()` → performs all combat initialization:
   - Initializes battlefield with platform geometry
   - Creates and initializes BattlefieldView component
   - Calls `ICameraService.SwitchToCombatCamera()`
4. `CameraService` switches camera priorities and starts rotation coroutine

### Testability

To unit test states without actual cameras:
1. Create mock implementation of `ICameraService`
2. Inject mock into `CombatActiveState`
3. Verify `SwitchToCombatCamera()` and `SwitchToIsometricCamera()` are called

## Customization

### Adjusting Camera Behavior

Edit `CameraConfig` asset to change:
- **Orthographic Size**: Zoom level (lower = closer)
- **Damping**: How tightly camera follows target (lower = snappier)
- **Transition Time**: Duration of rotation animation
- **Rotation Angles**: Camera angles for each mode

### Different Combat Cameras per Platform

Currently, all combat platforms share one combat camera configuration. To have platform-specific cameras:

1. Create multiple CameraConfig assets
2. Modify `CombatPlatform` to accept different configs
3. Pass config reference to `CameraService` dynamically

### Adding Camera Shake or Effects

The CameraService can be extended with additional methods:
```csharp
public interface ICameraService
{
    void SwitchToCombatCamera(float transitionTime = -1f);
    void SwitchToIsometricCamera(float transitionTime = -1f);
    bool IsTransitioning { get; }
    
    // Extensions
    void ShakeCamera(float intensity, float duration);
    void ZoomCamera(float targetSize, float duration);
}
```

## Scene Checklist

Before committing scene changes, verify:

- [ ] IsometricCamera exists with Priority 10, rotation X:30 Y:45
- [ ] CombatCamera exists with Priority 0, rotation X:45 Y:90
- [ ] CameraService GameObject exists with component attached
- [ ] Both cameras assigned in CameraService Inspector
- [ ] CameraConfig assigned in CameraService Inspector
- [ ] CameraConfig asset exists in Resources folder
- [ ] SceneContext has GameInstaller in Mono Installers
- [ ] Tested in Play Mode: smooth transitions work
- [ ] Console shows no errors related to camera or injection

## Files Modified/Created

### New Files
- `Scripts/Core/Camera/CameraConfig.cs` - ScriptableObject configuration
- `Scripts/Core/Camera/ICameraService.cs` - Service interface
- `Scripts/Core/Camera/CameraService.cs` - Service implementation

### Modified Files
- `Scripts/Platform/States/CombatActiveState.cs` - Merged state handling battlefield initialization and camera management
- `Scripts/Platform/Implementations/CombatPlatform.cs` - Creates single CombatActiveState with controller and camera service
- `Scripts/Core/DI/GameInstaller.cs` - Adds camera service bindings

### Deleted Files
- `Scripts/Platform/States/CombatPlatformActiveState.cs` - Merged into CombatActiveState to fix battlefield lifecycle bug

### Scene Assets
- `Resources/CameraConfig.asset` - Configuration asset
- `Scenes/Area.unity` - Scene with cameras and CameraService

## Support

If you encounter issues not covered in this guide:
1. Check Unity Console for detailed error messages
2. Verify all bindings in GameInstaller
3. Use Debug.Log in CameraService to trace execution
4. Ensure Cinemachine package version is compatible

