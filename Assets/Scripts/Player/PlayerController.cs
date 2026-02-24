using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using System;
using Mirror.Transports.Encryption;

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
    [SerializeField]
    private float jumpHeight = 1.5f;

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

    [Header("Item Carrying")]
    [SerializeField]
    private Transform itemHolder;
    [SerializeField]
    private float pickupRadius = 1.2f;

    public Transform ItemHolder => itemHolder;

    private bool _jumpPressed;
    private bool _sprintHeld;
    private CarryableObject _carriedObject;
    private CharacterController _characterController;
    private float _verticalVelocity;
    private float _verticalRotation;
    private Vector2 _moveInput;
    private Vector2 _lookInput;

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

    public void OnJump()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        _jumpPressed = true;
    }

    public void OnSprint(InputValue value)
    {
        if (!isLocalPlayer)
        {
            return;
        }

        _sprintHeld = value.isPressed;
    }

    public void OnInteract()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (_carriedObject == null)
        {
            TryInteract();
        }
    }

    public void OnDrop()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        if (_carriedObject != null)
        {
            DropItem();
        }
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
        transform.Rotate(Vector3.up * _lookInput.x * mouseSensitivity);

        _verticalRotation -= _lookInput.y * mouseSensitivity;
        _verticalRotation = Mathf.Clamp(_verticalRotation, -verticalLookLimit, verticalLookLimit);
        cameraHolder.localRotation = Quaternion.Euler(_verticalRotation, 0f, 0f);
    }

    private void HandleMovement()
    {
        Vector3 moveDir = (transform.forward * _moveInput.y + transform.right * _moveInput.x).normalized;
        float speed = _sprintHeld ? sprintMultiplier * moveSpeed : moveSpeed;

        if (_characterController.isGrounded)
        {
            if (_verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }

            if (_jumpPressed)
            {
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
        }

        _jumpPressed = false;

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

    private void TryInteract()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, pickupRadius);

        InteractableObject closest = null;
        float shortestDistance = float.MaxValue;

        foreach (Collider collider in hits)
        {
            InteractableObject item = collider.GetComponent<InteractableObject>();

            if (item == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, collider.transform.position);

            if (distance < shortestDistance) {
                shortestDistance = distance;
                closest = item;
            }
        }

        if (closest != null)
        {
            closest.OnInteract(this);
        }
    }

    public void PickupItem(CarryableObject item)
    {
        if (_carriedObject != null)
        {
            return;
        }

        CommandPickupItem(item.netIdentity);
    }

    [Command]
    private void CommandPickupItem(NetworkIdentity itemIdentity)
    {
        CarryableObject item = itemIdentity.GetComponent<CarryableObject>();

        if (item == null || item.IsCarried)
        {
            return;
        }

        item.PickedUpBy(netIdentity);
        RpcAttachItem(itemIdentity);
    }

    [ClientRpc]
    private void RpcAttachItem(NetworkIdentity itemIdentity)
    {
        CarryableObject item = itemIdentity.GetComponent<CarryableObject>();

        if (item == null)
        {
            return;
        }

        _carriedObject = item;
        item.GetComponent<Rigidbody>().isKinematic = true;
        item.GetComponent<Rigidbody>().detectCollisions = false;
        item.transform.SetParent(itemHolder, false);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;
    }

    private void DropItem()
    {
        if (_carriedObject == null)
        {
            return;
        }

        CommandDropItem(_carriedObject.netIdentity);
    }

    [Command]
    private void CommandDropItem(NetworkIdentity itemIdentity)
    {
        CarryableObject item = itemIdentity.GetComponent<CarryableObject>();

        if (item == null)
        {
            return;
        }

        item.Dropped();
        RpcDetachItem(itemIdentity);
    }

    [ClientRpc]
    private void RpcDetachItem(NetworkIdentity itemIdentity)
    {
        CarryableObject item = itemIdentity.GetComponent<CarryableObject>();

        if (item == null)
        {
            return;
        }

        item.GetComponent<Rigidbody>().isKinematic = false;
        item.GetComponent<Rigidbody>().detectCollisions = true;
        item.transform.SetParent(null);
        item.transform.position = transform.position + transform.forward * 0.6f + Vector3.up * 0.1f;
        _carriedObject = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
}
