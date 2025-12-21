using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MovementController : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference dashAction;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashDistance = 3f;
    public float dashHeightOffset = 1f;
    public float gravity = -9.81f;
    public float landingDepth = 0.9f;
    public float wallMargin = 0.05f;
    public LayerMask wallLayer;

    private CharacterController cc;
    private Vector3 velocity;

    private PlatformGraphRegistry.Node currentNode;

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
        currentNode = PlatformGraphRegistry.Instance.GetNearestNode(transform.position);
        if (currentNode != null)
        {
            MoveToNode(currentNode);
        }
        else
        {
            Debug.LogWarning("No platform node found near character start position.");
        }
    }

    void Update()
    {
        HandleMove();

        if (dashAction?.action?.triggered == true)
        {
            HandleDash();
        }
    }

    void HandleMove()
    {
        Vector2 input = moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        if (moveDir.sqrMagnitude > 0.01f)
        {
            float dist = CheckWallDistance(moveDir, moveSpeed * Time.deltaTime, true);
            cc.Move(moveDir * dist);
        }

        if (cc.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    void HandleDash()
    {
        Vector2 input = moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector3 dashDir = new Vector3(input.x, 0f, input.y);

        if (dashDir.sqrMagnitude < 0.1f) return;
        dashDir.Normalize();

        // 1️⃣ Дэш внутри платформы (Raycast до стены)
        float distToWall = CheckWallDistance(dashDir, dashDistance, true);

        if (distToWall >= dashDistance)
        {
            Vector3 target = transform.position + dashDir * dashDistance;
            Teleport(target);
            Debug.Log($"[Dash] Inside platform: moved {dashDistance}, target={target}");
        }
        else
        {
            // 2️⃣ Попытка прыгнуть на соседнюю платформу
            PlatformGraphRegistry.Node next = PickNextPlatform(dashDir);

            if (next != null)
            {
                Vector3 landing = ComputeLandingOnNeighbor(next/*, dashDir*/);
                Teleport(landing);
                currentNode = next;
                Debug.Log($"[Dash] Jumped to neighbor platform: {next.id}, landing={landing}");
                Debug.DrawRay(transform.position + Vector3.up * 0.5f, dashDir * distToWall, Color.green, 2f);
                Debug.DrawLine(transform.position, landing, Color.yellow, 2f);
            }
            else
            {
                // Дэш к стене
                Vector3 target = transform.position + dashDir * (distToWall - wallMargin);
                Teleport(target);
                Debug.Log($"[Dash] Hit wall, moved to edge: {distToWall}, target={target}");
                Debug.DrawRay(transform.position + Vector3.up * 0.5f, dashDir * distToWall, Color.red, 2f);
            }
        }
    }

    float CheckWallDistance(Vector3 dir, float maxDist, bool drawDebug = false)
    {
        Ray ray = new Ray(transform.position + Vector3.up * 0.5f, dir);
        if (Physics.Raycast(ray, out RaycastHit hit, maxDist, wallLayer))
        {
            if (drawDebug)
                Debug.DrawRay(ray.origin, dir * hit.distance, Color.cyan, 0.1f);
            return Mathf.Max(0f, hit.distance - wallMargin);
        }

        if (drawDebug)
            Debug.DrawRay(ray.origin, dir * maxDist, Color.gray, 0.1f);

        return maxDist;
    }

    PlatformGraphRegistry.Node PickNextPlatform(Vector3 dashDir)
    {
        if (currentNode == null || currentNode.neighbors.Count == 0) return null;
        var strLog = "";
        foreach (PlatformGraphRegistry.Node n in currentNode.neighbors)
        {
            strLog += n.id + " ";
        }
        Debug.Log("Picking next platform from current node. Neighbors: " + strLog);

        PlatformGraphRegistry.Node best = null;
        float minAngle = 90f; // градусы

        foreach (var neigh in currentNode.neighbors)
        {
            Vector3 toNeighbor = (neigh.worldPos - transform.position);
            float angle = Vector3.Angle(dashDir, toNeighbor);

            if (angle < minAngle)
            {
                minAngle = angle;
                best = neigh;
            }
        }

        if (best != null)
            Debug.Log($"Selected neighbor platform {best.id} at angle {minAngle}");

        return best;
    }

    Vector3 ComputeLandingOnNeighbor(PlatformGraphRegistry.Node neighbor, float landingDepth = 0.7f)
    {
        Vector3 playerPos = transform.position;
        Vector3 center = neighbor.worldPos;

        // Ось плоскости (горизонтальное направление к центру)
        Vector3 xAxis = center - playerPos;
        xAxis.y = 0;
        if (xAxis.sqrMagnitude < 0.0001f)
            xAxis = Vector3.forward;
        xAxis.Normalize();

        // Вертикальная ось
        Vector3 yAxis = Vector3.up;

        // Нормаль плоскости
        Vector3 planeNormal = Vector3.Cross(xAxis, yAxis);

        Plane cutPlane = new Plane(planeNormal, playerPos);

        float bestDist = float.MaxValue;
        Vector3 bestPoint = playerPos;

        foreach (var wall in neighbor.platform.GetComponentsInChildren<Collider>())
        {
            MeshCollider mc = wall as MeshCollider;
            if (mc == null || mc.sharedMesh == null)
                continue;

            Mesh mesh = mc.sharedMesh;

            Vector3[] verts = mesh.vertices;
            int[] tris = mesh.triangles;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 v0 = mc.transform.TransformPoint(verts[tris[i]]);
                Vector3 v1 = mc.transform.TransformPoint(verts[tris[i+1]]);
                Vector3 v2 = mc.transform.TransformPoint(verts[tris[i+2]]);

                if (IntersectTriangleWithPlane(cutPlane, v0, v1, v2, out Vector3 hit))
                {
                    float dist = Vector3.Distance(playerPos, hit);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestPoint = hit;
                    }
                }
            }
        }

        // вычисляем направление "вглубь" платформы
        Vector3 toCenter = (center - bestPoint).normalized;

        // сдвигаем landing точку на глубину landingDepth, чтобы точно оказаться за стеной
        Vector3 landing = bestPoint + toCenter * landingDepth;

        // высота
        landing.y += dashHeightOffset;

        Debug.Log($"[LandingPlane] landing on platform {neighbor.id} at {landing}");

        return landing;
    }


    bool IntersectTriangleWithPlane(Plane plane, Vector3 v0, Vector3 v1, Vector3 v2, out Vector3 hit)
    {
        hit = Vector3.zero;

        Vector3[] verts = { v0, v1, v2 };

        for (int i = 0; i < 3; i++)
        {
            Vector3 a = verts[i];
            Vector3 b = verts[(i + 1) % 3];

            if (plane.Raycast(new Ray(a, b - a), out float t))
            {
                if (t >= 0 && t <= Vector3.Distance(a, b))
                {
                    hit = a + (b - a).normalized * t;
                    return true;
                }
            }
        }
        return false;
    }


    // helper: checks if 'child' is descendant of 'parent'
    bool IsChildOf(Transform child, Transform parent)
    {
        Transform t = child;
        while (t != null)
        {
            if (t == parent) return true;
            t = t.parent;
        }
        return false;
    }

    void Teleport(Vector3 pos)
    {
        cc.enabled = false;
        transform.position = pos;
        cc.enabled = true;
    }

    void MoveToNode(PlatformGraphRegistry.Node node)
    {
        cc.enabled = false;
        transform.position = node.worldPos + Vector3.up * dashHeightOffset;
        cc.enabled = true;
    }
}
