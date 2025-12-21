# Character Movement Fix Report

## Issues Identified

### Issue 1: Character Can Jump Outside Platform
**Problem:** Character could dash/jump outside the platform boundary, violating the constraint that the character must always stay on a platform.

**Root Cause:**
- The new `CharacterMovementController` used `CheckWallDistance()` which relies on Physics.Raycast with wall colliders
- This approach doesn't accurately detect platform boundaries for irregular shapes
- No validation was performed to ensure dash target positions were within the platform boundary
- The Demo implementation uses polygon intersection math which is more accurate

**Solution:**
1. Implemented `ComputeDistanceToPlatformEdge()` method that uses polygon intersection (same as Demo)
2. Added `IsPointInPlatformBoundary()` validation method to validate positions
3. Updated `HandleDash()` to:
   - Use `ComputeDistanceToPlatformEdge()` instead of `CheckWallDistance()` for dash logic
   - Validate dash target is within platform boundary before teleporting
   - Clamp dash distance to platform edge if target would be outside
4. Updated `HandleMove()` to also validate movement stays within platform boundary

### Issue 2: Character Positioned Behind Wall When Jumping to Neighbor
**Problem:** When jumping to a neighbor platform, the character was positioned behind the wall instead of in front of it on the platform surface.

**Root Cause:**
- The `ComputeLandingOnNeighbor()` method used a complex plane intersection approach
- It tried to move "into" the platform using `landingDepth`, but the direction calculation was incorrect
- The method didn't properly find the intersection point on the platform boundary edge
- The Demo implementation uses ray-polygon intersection which directly finds the edge intersection point

**Solution:**
1. Replaced `ComputeLandingOnNeighbor()` with `ComputeLandingOnPlatform()` (matching Demo implementation)
2. Implemented proper ray-polygon intersection to find exact boundary edge intersection
3. Added `ProjectOntoPolygon()` fallback for cases where no direct intersection is found
4. Added `ClosestPointRaySegment()` helper for projection calculations
5. Landing position is now calculated as the intersection point on the boundary edge, ensuring character lands on the platform surface, not behind walls

## Changes Made

### Files Modified:
1. **`Scripts/Character/CharacterMovementController.cs`**
   - Added `platformJumpRange` field (default 6f)
   - Added `ComputeDistanceToPlatformEdge()` method (polygon intersection)
   - Added `IsPointInPlatformBoundary()` validation method
   - Replaced `ComputeLandingOnNeighbor()` with `ComputeLandingOnPlatform()` (ray-polygon intersection)
   - Added `ProjectOntoPolygon()` method
   - Added `ClosestPointRaySegment()` helper method
   - Updated `HandleDash()` to use polygon-based edge detection and validation
   - Updated `HandleMove()` to validate movement stays within platform
   - Removed unused `IntersectTriangleWithPlane()` method

### Key Algorithm Changes:

#### Edge Distance Calculation:
```csharp
// OLD: Physics.Raycast (inaccurate for irregular shapes)
float distToWall = CheckWallDistance(dashDir, dashDistance, true);

// NEW: Polygon intersection (accurate)
float distToEdge = ComputeDistanceToPlatformEdge(dashDir);
```

#### Landing Position Calculation:
```csharp
// OLD: Plane intersection with depth offset (incorrect direction)
Vector3 landing = bestPoint + toCenter * landingDepth;

// NEW: Ray-polygon intersection (finds exact edge point)
Vector3 landing = ComputeLandingOnPlatform(start, dir, neighbor);
// Returns intersection point on boundary edge
```

## Testing Recommendations

1. **Test Dash Inside Platform:**
   - Dash in various directions on a platform
   - Verify character never leaves platform boundary
   - Verify character stops at edge when dash would go outside

2. **Test Jump to Neighbor:**
   - Dash toward neighbor platforms from different positions
   - Verify character lands on platform surface (not behind walls)
   - Verify character is positioned correctly relative to platform boundary

3. **Test Edge Cases:**
   - Dash at platform corners
   - Dash parallel to platform edges
   - Jump to platforms at different heights
   - Jump when very close to platform edge

## Technical Details

### Polygon Intersection Algorithm:
The `ComputeDistanceToPlatformEdge()` method uses 2D ray-polygon intersection:
- Projects 3D positions to XZ plane
- Tests ray against each edge of the platform boundary polygon
- Returns minimum distance to any intersecting edge

### Ray-Polygon Intersection for Landing:
The `ComputeLandingOnPlatform()` method:
- Finds intersection of dash ray with platform boundary edges
- Uses determinant-based line intersection test
- Returns the closest intersection point on the boundary
- Falls back to projection onto closest edge if no intersection found

## Compatibility

The implementation now matches the Demo scene's `CharacterDashController` behavior:
- Same polygon intersection algorithms
- Same landing position calculation
- Same edge detection logic
- Maintains compatibility with the new `IPlatform` system

