using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class CharacterMoveController : MonoBehaviour
{
    private CharacterController characterController;
    [SerializeField] private float moveSpeed = 5f;

    
    public float gravity = -9.81f;
    private Vector3 velocity;
    
    // Ссылка на действие движения из Input Actions (в инспекторе присвойте действие типа Vector2)
    [SerializeField] private InputActionReference moveAction;

    void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    void OnEnable()
    {
        if (moveAction?.action != null) moveAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction?.action != null) moveAction.action.Disable();
    }

    void Update()
    {
        Vector2 input = moveAction?.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Vector3 moveDirection = new Vector3(input.x, 0f, input.y);

        if (moveDirection.magnitude > 1f) moveDirection.Normalize();

        characterController.Move(moveDirection * moveSpeed * Time.deltaTime);
        
        // Проверка касания земли
        if (characterController.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // маленькое отрицательное значение, чтобы "прилипать" к земле
        }

        // Гравитация
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }
}