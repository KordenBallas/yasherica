using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Platform;
using Core.Events;
using Character.Locomotion;
using Core.Logging;
using Zenject;

namespace Character
{
    /// <summary>
    /// Character movement controller that integrates with the new IPlatform system.
    /// Dash mechanic implemented exactly as in Demo CharacterDashController.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMovementController : MonoBehaviour, IMovementInputLock, ICharacterVelocityProvider
    {
    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference dashAction;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float dashDistance = 3f;
    public float dashHeightOffset = 1f;
    public float gravity = -9.81f;
    public float wallMargin = 0.05f;
    public LayerMask wallLayer;

    private CharacterController cc;
    private Vector3 velocity;
    private Vector3 _planarVelocity;
    [Inject] private IGameLogger _logger;

    private IPlatform currentPlatform;

    /// <summary>ICharacterVelocityProvider: world-space horizontal velocity intent this frame (zero when idle).</summary>
    public Vector3 PlanarVelocity => _planarVelocity;

    /// <summary>ICharacterVelocityProvider: top planar speed, used to normalize the run blend.</summary>
    public float MaxPlanarSpeed => moveSpeed;
    
    [Inject] private ICharacterRegistry _characterRegistry;

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

    /// <summary>
    /// IMovementInputLock adapter: toggles the input actions so movement and dash
    /// stop reacting while another system (e.g. the inventory) owns the screen.
    /// </summary>
    public void SetMovementEnabled(bool enabled)
    {
        if (enabled)
        {
            moveAction?.action?.Enable();
            dashAction?.action?.Enable();
        }
        else
        {
            moveAction?.action?.Disable();
            dashAction?.action?.Disable();
        }
    }

    void Start()
    {
        // Register with registry first (before any movement logic)
        if (_characterRegistry != null)
        {
            _characterRegistry.RegisterCharacter(transform);
            _logger?.Info(LogCategory.Character,"[CharacterMovementController] Self-registered with CharacterRegistry");
        }
        else
        {
            _logger?.Warning(LogCategory.Character,"[CharacterMovementController] CharacterRegistry not injected - character will not be available for combat");
        }
        
        /*currentNode = PlatformGraphRegistry.Instance.GetNearestNode(transform.position);
        if (currentNode != null)
        {
            MoveToNode(currentNode);
        }
        else
        {
            _logger?.Warning(LogCategory.Character,"No platform node found near character start position.");
        }*/
        
        // Find initial platform
        UpdateCurrentPlatform();
        if (currentPlatform != null)
        {
            MoveToPlatformInstant(currentPlatform);
            //CachePlatformShape();
        }
        else
        {
            _logger?.Warning(LogCategory.Character,"[CharacterMovementController] No platform found near character start position.");
        }
    }

    void OnDestroy()
    {
        // Unregister when destroyed
        if (_characterRegistry != null)
        {
            _characterRegistry.UnregisterCharacter(transform);
            _logger?.Info(LogCategory.Character,"[CharacterMovementController] Unregistered from CharacterRegistry");
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
            //CachePlatformShape();
            PlatformEvents.OnPlatformEntered?.Invoke(currentPlatform);
            PlatformEvents.OnPlatformChanged?.Invoke(oldPlatform, currentPlatform);
        }
    }
    
    void MoveToPlatformInstant(IPlatform platform)
    {
        if (platform?.Visual == null) return;

        cc.enabled = false;
        // Center CELL, not the raw centroid — a concave island's centroid can fall outside the pen.
        transform.position = PlatformAnchor.CenterCellWorld(platform.Visual.Surface, platform.Visual.Position)
            + Vector3.up * dashHeightOffset;
        cc.enabled = true;

        SetCurrentPlatform(platform);
    }

    void HandleMove()
    {
        Vector2 input = moveAction?.action?.ReadValue<Vector2>() ?? Vector2.zero;
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        // Expose the movement intent (direction * speed) for the locomotion presenter to
        // drive the run blend and facing. Analog input under full deflection scales the blend.
        _planarVelocity = moveDir.sqrMagnitude > 0.0001f ? moveDir * moveSpeed : Vector3.zero;

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
            _logger?.Info(LogCategory.Character,$"[Dash] Inside platform: moved {dashDistance}, target={target}");
        }
        else
        {
            // 2️⃣ Попытка прыгнуть на соседнюю платформу
            IPlatform next = PickNextPlatform(dashDir);

            if (next != null)
            {
                Vector3 landing = ComputeLandingOnNeighbor(next/*, dashDir*/);
                Teleport(landing);
                SetCurrentPlatform(next);
                _logger?.Info(LogCategory.Character,$"[Dash] Jumped to neighbor platform: {next.Id}, landing={landing}");
                Debug.DrawRay(transform.position + Vector3.up * 0.5f, dashDir * distToWall, Color.green, 2f);
                Debug.DrawLine(transform.position, landing, Color.yellow, 2f);
            }
            else
            {
                // Дэш к стене
                Vector3 target = transform.position + dashDir * (distToWall - wallMargin);
                Teleport(target);
                _logger?.Info(LogCategory.Character,$"[Dash] Hit wall, moved to edge: {distToWall}, target={target}");
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

    IPlatform PickNextPlatform(Vector3 dashDir)
    {
        if (currentPlatform == null || currentPlatform.Neighbors.Count == 0) return null;
        var strLog = "";
        foreach (IPlatform n in currentPlatform.Neighbors)
        {
            strLog += n.Id + " ";
        }
        _logger?.Info(LogCategory.Character,"Picking next platform from current node. Neighbors: " + strLog);

        IPlatform best = null;
        float minAngle = 90f; // градусы

        foreach (var neigh in currentPlatform.Neighbors)
        {
            Vector3 toNeighbor = (neigh.Visual.Position - transform.position);
            float angle = Vector3.Angle(dashDir, toNeighbor);

            if (angle < minAngle)
            {
                minAngle = angle;
                best = neigh;
            }
        }

        if (best != null)
            _logger?.Info(LogCategory.Character,$"Selected neighbor platform {best.Id} at angle {minAngle}");

        return best;
    }

    Vector3 ComputeLandingOnNeighbor(IPlatform neighbor)
    {
        // Nearest walkable CELL to the hero: a cell center sits a full hex inradius inside the wall
        // colliders, so the jump always lands in the pen. (The old mesh-edge probe measured the
        // decorative rim, which lies beyond the walls — its fixed inward push could land outside.)
        Vector3 landing = PlatformAnchor.NearestCellWorld(
            neighbor.Visual.Surface, neighbor.Visual.Position, transform.position);
        landing.y += dashHeightOffset;

        _logger?.Info(LogCategory.Character,$"[LandingPlane] landing on platform {neighbor.Id} at {landing}");

        return landing;
    }


    void Teleport(Vector3 pos)
    {
        cc.enabled = false;
        transform.position = pos;
        cc.enabled = true;
    }

    /*void MoveToNode(PlatformGraphRegistry.Node node)
    {
        cc.enabled = false;
        transform.position = node.worldPos + Vector3.up * dashHeightOffset;
        cc.enabled = true;
    }*/
}
}
