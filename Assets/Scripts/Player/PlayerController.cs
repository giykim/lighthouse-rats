using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 4f;
    [SerializeField]
    private float sprintMultiplier = 1.5f;
    [SerializeField]
    private float gravity = -20f;

    [Header("Mouse Look")]
    [SerializeField]
    private Transform cameraHolder;
    [SerializeField]
    private float mouseSensitivity = 0.1f;
    [SerializeField]
    private float verticalLookLimit = 80f;

    [Header("Camera")]
    [SerializeField]
    private Transform cameraTarget;

    private CharacterController _characterController;
    private float _verticalVelocity;
    private float _verticalRotation;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _sprintHeld;

    private Vector3 UP_VECTOR = Vector3.up;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    public override void OnStartLocalPlayer()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GetComponent<PlayerInput>().enabled = true;

        if (cameraHolder != null)
        {
            Camera camera = cameraHolder.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                camera.enabled = true;
            }

            AudioListener audioListener = camera.GetComponentInChildren<AudioListener>();
            if (audioListener != null)
            {
                audioListener.enabled = true;
            }
        }
    }

    public void OnMove(InputValue value)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        _moveInput = value.Get<Vector2>();
    }

    public void OnLook(InputValue value)
    {
        if (!isLocalPlayer) {
            return;
        }

        _lookInput = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        if (!isLocalPlayer) {
            return;
        }

        _sprintHeld = value.isPressed;
    }

    public void OnCursorUnlock(InputValue value)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (value.isPressed)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        HandleMouseLook();
        HandleMovement();
        HandleCursorRelock();
    }

    private void HandleMouseLook()
    {
        transform.Rotate(UP_VECTOR * _lookInput.x * mouseSensitivity);

        _verticalRotation -= _lookInput.y * mouseSensitivity;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraHolder.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector3 moveDir = (transform.forward * _moveInput.y + transform.right * _moveInput.x).normalized;
        float speed = _sprintHeld ? sprintMultiplier * moveSpeed : moveSpeed;

        if (_characterController.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f;

        _verticalVelocity += gravity * Time.deltaTime;

        _characterController.Move((moveDir * speed + Vector3.up * _verticalVelocity) * Time.deltaTime);
    }

    private void HandleCursorRelock()
    {
        if (Mouse.current != null
            && Mouse.current.leftButton.wasPressedThisFrame
            && Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
