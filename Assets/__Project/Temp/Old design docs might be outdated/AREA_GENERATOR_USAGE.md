# AreaGenerator GameObject Creation

## How It Works

When you call `areaGenerator.Generate()`, it now:

1. **Creates Area GameObject** - Parent GameObject named "Area" in the scene
2. **Creates Platform Instances** - Creates all `IPlatform` instances from the graph
3. **Creates Platform GameObjects** - For each platform, creates a GameObject with:
   - `PlatformView` component
   - Mesh (low-poly platform with jagged edges)
   - MeshCollider (for the platform mesh)
   - Floor Collider (BoxCollider for walking)
   - Wall Colliders (BoxCollider for each edge segment)
4. **Manages Visibility** - Only platforms in visible range (n forward, m backward) are active

## Platform Structure

Each platform GameObject has this hierarchy:
```
Platform_X
├── Mesh (MeshFilter + MeshRenderer + MeshCollider)
└── PlatformCollider
    ├── FloorCollider (BoxCollider)
    └── WallCollider_0, WallCollider_1, ... (BoxColliders)
```

## Visibility Management

- All platform GameObjects are created but initially **inactive**
- When `SetVisibleRange()` is called, only platforms in range are **activated**
- As character moves, `UpdateVisiblePlatforms()` is called to update visibility
- Platforms outside range are deactivated (pooled), platforms in range are activated (unpooled)

## Example Usage

```csharp
// Generate area
var areaGenerator = new AreaGenerator(graph, noiseMap);
areaGenerator.Generate();

// You should now see:
// - "Area" GameObject in hierarchy
// - "Platform_0", "Platform_1", etc. as children
// - Only visible platforms are active (others are inactive)
```

## Troubleshooting

If you don't see platforms:
1. Check that `areaGenerator.Generate()` is being called
2. Check the "Area" GameObject in hierarchy
3. Check that entry platform exists: `areaGenerator.EntryPlatform != null`
4. Verify platforms are in visible range (entry + 3 forward, 1 backward)
5. Check that platform GameObjects are being created (they may be inactive)

