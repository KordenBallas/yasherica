using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CharacterDashController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference dashAction;
    [SerializeField] private InputActionReference moveAction;

    [Header("Dash Settings")]
    public float dashRange = 3f;                   // dash inside current platform
    public float platformJumpRange = 6f;           // dash to next platform
    public float dashHeightOffset = 1f;
    public float dashDelay = 0.05f;
    public float minInput = 0.2f;

    private CharacterController cc;
    private PlatformGraphRegistry.Node currentNode;

    // Cached polygon
    private readonly List<Vector3> platformTopVerts = new();

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        dashAction?.action?.Enable();
        moveAction?.action?.Enable();
    }

    void OnDisable()
    {
        dashAction?.action?.Disable();
        moveAction?.action?.Disable();
    }

    void Start()
    {
        currentNode = PlatformGraphRegistry.Instance.GetNearestNode(transform.position);

        if (currentNode != null)
        {
            MoveToNodeInstant(currentNode);
            CachePlatformShape();
        }
    }

    void Update()
    {
        if (dashAction.action.triggered)
            TryDash();
    }

    /* =========================================================
       DASH MAIN LOGIC
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
       DASH CASES
       ========================================================= */

    IEnumerator DashInsidePlatform(Vector3 dashDir)
    {
        yield return new WaitForSeconds(dashDelay);

        Vector3 target = transform.position + dashDir * dashRange;

        cc.enabled = false;
        transform.position = target;
        cc.enabled = true;
    }

    IEnumerator DashToEdge(Vector3 dashDir, float distToEdge)
    {
        yield return new WaitForSeconds(dashDelay);

        Vector3 target = transform.position + dashDir * distToEdge;

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

        currentNode = next;
        CachePlatformShape();
    }

    /* =========================================================
       SELECT NEXT PLATFORM
       ========================================================= */

    PlatformGraphRegistry.Node PickTargetPlatform(Vector3 dir)
    {
        if (currentNode == null || currentNode.neighbors.Count == 0)
            return null;

        PlatformGraphRegistry.Node best = null;
        float bestDot = 0.3f;    // minimal forwardness

        foreach (var neigh in currentNode.neighbors)
        {
            Vector3 ndir = (neigh.worldPos - currentNode.worldPos).normalized;
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
        platformTopVerts.AddRange(currentNode.topBoundary);
    }

    /* =========================================================
       RAY–POLYGON INTERSECTION IN XZ (no raycast)
       ========================================================= */

    float ComputeDistanceToPlatformEdge(Vector3 dashDir)
    {
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
            float u = ((A2.x - P.x) * D.y     - (A2.y - P.y) * D.x) / det;

            if (t > 0 && u >= 0 && u <= 1)
                best = Mathf.Min(best, t);
        }

        return best;
    }

    /* =========================================================
       COMPUTE LANDING ON NEXT PLATFORM
       ========================================================= */

    Vector3 ComputeLandingOnPlatform(Vector3 start, Vector3 dir, PlatformGraphRegistry.Node next)
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
            float u = ((A2.x - P.x) * D.y     - (A2.y - P.y) * D.x) / det;

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

        float y = next.worldPos.y;

        if (hitFound)
            return new Vector3(bestHit.x, y, bestHit.y);

        // If no exact intersection — project onto closest edge
        return ProjectOntoPolygon(P, D, verts, y);
    }

    List<Vector3> GetPlatformTopPolygon(PlatformGraphRegistry.Node node)
    {
        return node.topBoundary;
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

    void MoveToNodeInstant(PlatformGraphRegistry.Node node)
    {
        cc.enabled = false;
        transform.position = node.worldPos + Vector3.up * dashHeightOffset;
        cc.enabled = true;
    }
}