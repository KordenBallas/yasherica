using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Platform;
using Core.Events;

namespace Character
{
    /// <summary>
    /// Character movement controller that integrates with the new IPlatform system.
    /// Dash mechanic implemented exactly as in Demo CharacterDashController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class AICharacterMovementController : MonoBehaviour
    {
        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference dashAction;

        [Header("Movement Settings")]
        public float moveSpeed = 5f;
        public float gravity = -9.81f;
        private Vector3 velocity;

        [Header("Dash Settings")]
        public float dashRange = 3f;                   // dash inside current platform
        public float platformJumpRange = 6f;           // dash to next platform
        public float dashHeightOffset = 1f;
        public float dashDelay = 0.05f;
        public float minInput = 0.2f;

        private CharacterController cc;
        private IPlatform currentPlatform;

        // Cached polygon (in world space)
        private readonly List<Vector3> platformTopVerts = new();

        void Awake()
        {
            cc = GetComponent<CharacterController>();
        }

        void OnEnable()
        {
            moveAction?.action?.Enable();
            dashAction?.action?.Enable();
        }

        void OnDisable()
        {
            moveAction?.action?.Disable();
            dashAction?.action?.Disable();
        }

        void Start()
        {
            // Find initial platform
            UpdateCurrentPlatform();
            if (currentPlatform != null)
            {
                MoveToPlatformInstant(currentPlatform);
                CachePlatformShape();
            }
            else
            {
                Debug.LogWarning("[CharacterMovementController] No platform found near character start position.");
            }
        }

        void Update()
        {
            HandleMove();

            if (dashAction?.action?.triggered == true)
            {
                TryDash();
            }
        }

        void HandleMove()
        {
            Vector2 input = moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector3 moveDir = new Vector3(input.x, 0f, input.y);

            if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

            if (moveDir.sqrMagnitude > 0.01f)
            {
                cc.Move(moveDir * moveSpeed * Time.deltaTime);
            }

            if (cc.isGrounded && velocity.y < 0) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            cc.Move(velocity * Time.deltaTime);
        }

        /* =========================================================
           DASH MAIN LOGIC (exactly as Demo)
           ========================================================= */

        void TryDash()
        {
            Vector2 raw = moveAction?.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
            Vector3 dashDir = new(raw.x, 0, raw.y);

            if (dashDir.sqrMagnitude < minInput)
                return;

            dashDir.Normalize();

            float distToEdge = ComputeDistanceToPlatformEdge(dashDir);

            // A — dash inside platform
            if (distToEdge > dashRange)
            {
                StartCoroutine(DashInsidePlatform(dashDir));
                return;
            }

            // B — try jump to other platform (ON SAME LINE)
            if (distToEdge <= dashRange && distToEdge < platformJumpRange)
            {
                StartCoroutine(DashToNextPlatform(dashDir));
                return;
            }

            // C — stop at edge, no falling
            StartCoroutine(DashToEdge(dashDir, distToEdge));
        }

        /* =========================================================
           DASH CASES (exactly as Demo)
           ========================================================= */

        IEnumerator DashInsidePlatform(Vector3 dashDir)
        {
            yield return new WaitForSeconds(dashDelay);

            // Recompute edge distance to ensure accuracy
            float distToEdge = ComputeDistanceToPlatformEdge(dashDir);
            
            // Clamp dash distance to edge to prevent going outside
            float actualDashDist = Mathf.Min(dashRange, distToEdge);
            
            // If edge distance is invalid, don't move
            if (distToEdge >= float.MaxValue * 0.5f)
            {
                yield break;
            }
            
            Vector3 target = transform.position + dashDir * actualDashDist;
            
            // Final validation - ensure target is within boundary
            if (!IsPointInPlatformBoundary(target) && platformTopVerts.Count >= 3)
            {
                // Clamp to nearest point on boundary
                target = ClampToPlatformBoundary(target);
            }

            cc.enabled = false;
            transform.position = target;
            cc.enabled = true;
        }

        IEnumerator DashToEdge(Vector3 dashDir, float distToEdge)
        {
            yield return new WaitForSeconds(dashDelay);

            // Validate distToEdge is reasonable
            if (distToEdge >= float.MaxValue * 0.5f || distToEdge < 0)
            {
                // Don't move if edge distance is invalid
                yield break;
            }

            Vector3 target = transform.position + dashDir * distToEdge;
            
            // Ensure target is within platform boundary (should be on edge, but validate)
            if (!IsPointInPlatformBoundary(target) && platformTopVerts.Count >= 3)
            {
                // Project to nearest point on boundary
                target = ClampToPlatformBoundary(target);
            }

            cc.enabled = false;
            transform.position = target;
            cc.enabled = true;
        }

        IEnumerator DashToNextPlatform(Vector3 dashDir)
        {
            var next = PickTargetPlatform(dashDir);
            if (next == null)
                yield break;

            // Compute exact landing point on polygon
            Vector3 landing = ComputeLandingOnPlatform(transform.position, dashDir, next);

            yield return new WaitForSeconds(dashDelay);

            cc.enabled = false;
            transform.position = landing + Vector3.up * dashHeightOffset;
            cc.enabled = true;

            SetCurrentPlatform(next);
            CachePlatformShape();
        }

        /* =========================================================
           SELECT NEXT PLATFORM (exactly as Demo - using dot product)
           ========================================================= */

        IPlatform PickTargetPlatform(Vector3 dir)
        {
            if (currentPlatform == null || currentPlatform.Neighbors.Count == 0)
                return null;

            IPlatform best = null;
            float bestDot = 0.3f;    // minimal forwardness

            Vector3 currentPos = currentPlatform.Visual != null ? currentPlatform.Visual.Position : transform.position;

            foreach (var neigh in currentPlatform.Neighbors)
            {
                if (neigh.Visual == null) continue;

                Vector3 ndir = (neigh.Visual.Position - currentPos).normalized;
                float dot = Vector3.Dot(dir, ndir);

                if (dot > bestDot)
                {
                    bestDot = dot;
                    best = neigh;
                }
            }

            return best;
        }

        /* =========================================================
           PLATFORM GEOMETRY
           ========================================================= */

        void CachePlatformShape()
        {
            platformTopVerts.Clear();
            if (currentPlatform?.Visual?.TopBoundary != null)
            {
                var boundary = currentPlatform.Visual.TopBoundary;
                Vector3 platformPos = currentPlatform.Visual.Position;

                // Transform boundary from local space to world space
                foreach (var localPoint in boundary)
                {
                    Vector3 worldPoint = platformPos + localPoint;
                    platformTopVerts.Add(worldPoint);
                }
            }
        }

        /* =========================================================
           RAY–POLYGON INTERSECTION IN XZ (exactly as Demo)
           ========================================================= */

        float ComputeDistanceToPlatformEdge(Vector3 dashDir)
        {
            if (platformTopVerts.Count < 3)
                return float.MaxValue;

            Vector2 P = new Vector2(transform.position.x, transform.position.z);
            Vector2 D = new Vector2(dashDir.x, dashDir.z);

            float best = float.MaxValue;

            for (int i = 0; i < platformTopVerts.Count; i++)
            {
                Vector3 A = platformTopVerts[i];
                Vector3 B = platformTopVerts[(i + 1) % platformTopVerts.Count];

                Vector2 A2 = new Vector2(A.x, A.z);
                Vector2 B2 = new Vector2(B.x, B.z);
                Vector2 E = B2 - A2;

                float det = D.x * (-E.y) - D.y * (-E.x);
                if (Mathf.Abs(det) < 1e-6f) continue;

                float t = ((A2.x - P.x) * (-E.y) - (A2.y - P.y) * (-E.x)) / det;
                float u = ((A2.x - P.x) * D.y - (A2.y - P.y) * D.x) / det;

                if (t > 0 && u >= 0 && u <= 1)
                    best = Mathf.Min(best, t);
            }

            return best;
        }

        /* =========================================================
           COMPUTE LANDING ON NEXT PLATFORM (exactly as Demo)
           ========================================================= */

        Vector3 ComputeLandingOnPlatform(Vector3 start, Vector3 dir, IPlatform next)
        {
            var verts = GetPlatformTopPolygon(next);
            Vector2 P = new(start.x, start.z);
            Vector2 D = new(dir.x, dir.z);

            float bestT = float.MaxValue;
            Vector2 bestHit = Vector2.zero;
            bool hitFound = false;

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 A = verts[i];
                Vector3 B = verts[(i + 1) % verts.Count];

                Vector2 A2 = new(A.x, A.z);
                Vector2 B2 = new(B.x, B.z);
                Vector2 E = B2 - A2;

                float det = D.x * (-E.y) - D.y * (-E.x);
                if (Mathf.Abs(det) < 1e-6f) continue;

                float t = ((A2.x - P.x) * (-E.y) - (A2.y - P.y) * (-E.x)) / det;
                float u = ((A2.x - P.x) * D.y - (A2.y - P.y) * D.x) / det;

                if (t > 0 && u >= 0 && u <= 1)
                {
                    hitFound = true;
                    if (t < bestT)
                    {
                        bestT = t;
                        bestHit = P + D * t;
                    }
                }
            }

            float y = next.Visual != null ? next.Visual.Position.y : start.y;

            if (hitFound)
                return new Vector3(bestHit.x, y, bestHit.y);

            // If no exact intersection — project onto closest edge
            return ProjectOntoPolygon(P, D, verts, y);
        }

        List<Vector3> GetPlatformTopPolygon(IPlatform platform)
        {
            if (platform?.Visual?.TopBoundary == null)
                return new List<Vector3>();

            var boundary = platform.Visual.TopBoundary;
            Vector3 platformPos = platform.Visual.Position;
            var worldVerts = new List<Vector3>();

            // Transform to world space
            foreach (var localPoint in boundary)
            {
                worldVerts.Add(platformPos + localPoint);
            }

            return worldVerts;
        }

        Vector3 ProjectOntoPolygon(Vector2 P, Vector2 D, List<Vector3> verts, float y)
        {
            float best = float.MaxValue;
            Vector3 bestPos = Vector3.zero;

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 A = verts[i];
                Vector3 B = verts[(i + 1) % verts.Count];

                // Compute closest point of the infinite ray to segment
                Vector3 cp = ClosestPointRaySegment(P, D, A, B);

                float d = (cp - new Vector3(P.x, y, P.y)).sqrMagnitude;
                if (d < best)
                {
                    best = d;
                    bestPos = cp;
                }
            }

            return bestPos;
        }

        Vector3 ClosestPointRaySegment(Vector2 P, Vector2 D, Vector3 A, Vector3 B)
        {
            // Implementation: projection of segment to ray, clamp along segment
            Vector2 A2 = new(A.x, A.z);
            Vector2 B2 = new(B.x, B.z);
            Vector2 E = B2 - A2;

            float segLenSq = E.sqrMagnitude;
            if (segLenSq < 1e-5f) return new Vector3(A2.x, A.y, A2.y);

            float t = Vector2.Dot((P - A2), E) / segLenSq;
            t = Mathf.Clamp01(t);

            Vector2 p = A2 + E * t;
            return new Vector3(p.x, A.y, p.y);
        }

        /* =========================================================
           INITIAL TELEPORT
           ========================================================= */

        void MoveToPlatformInstant(IPlatform platform)
        {
            if (platform?.Visual == null) return;

            cc.enabled = false;
            transform.position = platform.Visual.Position + Vector3.up * dashHeightOffset;
            cc.enabled = true;

            SetCurrentPlatform(platform);
        }

        void UpdateCurrentPlatform()
        {
            var registry = PlatformRegistry.Instance;
            if (registry == null) return;

            var platformAtPosition = registry.GetPlatformAtPosition(transform.position);
            if (platformAtPosition != null && platformAtPosition != currentPlatform)
            {
                SetCurrentPlatform(platformAtPosition);
            }
        }

        void SetCurrentPlatform(IPlatform newPlatform)
        {
            if (newPlatform == currentPlatform) return;

            var oldPlatform = currentPlatform;

            // Exit old platform
            if (oldPlatform != null)
            {
                oldPlatform.Exit();
                PlatformEvents.OnPlatformExited?.Invoke(oldPlatform);
            }

            // Enter new platform
            currentPlatform = newPlatform;
            if (currentPlatform != null)
            {
                currentPlatform.Enter();
                CachePlatformShape();
                PlatformEvents.OnPlatformEntered?.Invoke(currentPlatform);
                PlatformEvents.OnPlatformChanged?.Invoke(oldPlatform, currentPlatform);
            }
        }

        /// <summary>
        /// Checks if a point is within the current platform boundary (XZ plane).
        /// </summary>
        bool IsPointInPlatformBoundary(Vector3 point)
        {
            if (platformTopVerts.Count < 3) return false;

            bool inside = false;
            for (int i = 0, j = platformTopVerts.Count - 1; i < platformTopVerts.Count; j = i++)
            {
                Vector3 pi = platformTopVerts[i];
                Vector3 pj = platformTopVerts[j];

                if (((pi.z > point.z) != (pj.z > point.z)) &&
                    (point.x < (pj.x - pi.x) * (point.z - pi.z) / (pj.z - pi.z) + pi.x))
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        /// <summary>
        /// Clamps a point to the nearest point on the platform boundary.
        /// </summary>
        Vector3 ClampToPlatformBoundary(Vector3 point)
        {
            if (platformTopVerts.Count < 3) return point;

            float bestDist = float.MaxValue;
            Vector3 bestPoint = point;

            // Find closest point on any edge
            for (int i = 0; i < platformTopVerts.Count; i++)
            {
                Vector3 A = platformTopVerts[i];
                Vector3 B = platformTopVerts[(i + 1) % platformTopVerts.Count];

                Vector3 closest = ClosestPointOnSegment(point, A, B);
                float dist = Vector3.Distance(point, closest);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPoint = closest;
                }
            }

            return bestPoint;
        }

        /// <summary>
        /// Finds the closest point on a line segment to a given point.
        /// </summary>
        Vector3 ClosestPointOnSegment(Vector3 point, Vector3 segStart, Vector3 segEnd)
        {
            Vector3 segDir = segEnd - segStart;
            float segLenSq = segDir.sqrMagnitude;
            if (segLenSq < 1e-5f) return segStart;

            Vector3 toPoint = point - segStart;
            float t = Vector3.Dot(toPoint, segDir) / segLenSq;
            t = Mathf.Clamp01(t);

            return segStart + segDir * t;
        }
    }
}
