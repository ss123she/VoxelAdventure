using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMover : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float sensitivity = 10f;
    [SerializeField] private float maxVerticalAngle = 90f;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference lookAction;
    [SerializeField] private InputActionReference switchStateAction;

    private bool _isAutoMoving = true;
    private float _pitch;
    private float _yaw;

    private void Awake()
    {
        Vector3 angles = transform.eulerAngles;
        _pitch = angles.x;
        _yaw = angles.y;
    }

    private void OnEnable()
    {
        moveAction.action.Enable();
        lookAction.action.Enable();
        switchStateAction.action.Enable();
    }

    private void OnDisable()
    {
        moveAction.action.Disable();
        lookAction.action.Disable();
        switchStateAction.action.Disable();
    }

    private void Update()
    {
        if (switchStateAction.action.WasPressedThisFrame())
        {
            _isAutoMoving = !_isAutoMoving;
        }

        if (_isAutoMoving)
        {
            transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
        }
        else
        {
            HandleRotation();
            HandleMovement();
        }
    }

    private void HandleRotation()
    {
        Vector2 mouseDelta = lookAction.action.ReadValue<Vector2>();

        _yaw += mouseDelta.x * sensitivity * Time.deltaTime;
        _pitch -= mouseDelta.y * sensitivity * Time.deltaTime;
        _pitch = Mathf.Clamp(_pitch, -maxVerticalAngle, maxVerticalAngle);

        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }

    private void HandleMovement()
    {
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        Vector3 move = new Vector3(input.x, 0, input.y);
        transform.Translate(move * moveSpeed * Time.deltaTime);
    }
}