using Mirror;
using Mirror.Transports.Encryption;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static CarryableObject;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : NetworkBehaviour
{
    public CharacterController CharacterController => _characterController;
    public Vector2 MoveInput => _moveInput;

    [Header("Body")]
    [SerializeField]
    private GameObject bodyMesh;

    [Header("Movement")]
    [SerializeField]
    private float moveSpeed = 4f;
    [SerializeField]
    private float pushForce = 3f;
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
    private Transform itemAnchor;
    [SerializeField]
    private Transform storageAnchor;
    [SerializeField]
    private float interactDistance = 2.5f;

    [Header("Climbing")]
    [SerializeField]
    private ClimbableObject _currentClimbable;
    [SerializeField]
    private bool _isClimbing = false;

    public Transform ItemAnchor => itemAnchor;
    public Transform StorageAnchor => storageAnchor;

    private bool _jumpPressed;
    private bool _sprintHeld;
    private bool _isDragging;
    private CarryableObject _carriedObject;
    private CarryableObject _storedObject;
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

        Renderer[] bodyRenderers = bodyMesh.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in bodyRenderers)
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

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

        if (_isClimbing)
        {
            StopClimbing();
            return;
        }

        TryInteract();
    }

    public void OnSwap()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        CommandSwapItems();
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

    public void SetDragging(bool dragging)
    {
        _isDragging = dragging;
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

    public void StartClimbing(ClimbableObject climbable)
    {
        _isClimbing = true;
        _currentClimbable = climbable;
        _verticalVelocity = 0f;

        if (!isServer && climbable is Rope rope)
        {
            CommandSetRopeCollisionsIgnored(rope.netIdentity, true);
        }
    }

    public void StopClimbing()
    {
        if (_currentClimbable is Rope rope)
        {
            rope.SetSegmentCollisionsIgnored(_characterController, false);
            if (!isServer)
            {
                CommandSetRopeCollisionsIgnored(rope.netIdentity, false);
            }
        }

        _isClimbing = false;
        _currentClimbable = null;
        _verticalVelocity = 0f;
    }

    [Command]
    private void CommandSetRopeCollisionsIgnored(NetworkIdentity ropeIdentity, bool ignore)
    {
        Rope rope = ropeIdentity.GetComponent<Rope>();
        if (rope != null)
        {
            rope.SetSegmentCollisionsIgnored(_characterController, ignore);
        }
    }

    [Command]
    public void CommandApplyForceToSegment(NetworkIdentity segmentIdentity, Vector3 force)
    {
        Rigidbody rigidbody = segmentIdentity.GetComponent<Rigidbody>();
        if (rigidbody != null && !rigidbody.isKinematic)
            rigidbody.AddForce(force, ForceMode.Force);
    }

    private void Update()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        HandleMouseLook();

        if (_isClimbing)
        {
            HandleClimbing();
        } else
        {
            HandleMovement();
        }

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
        if (_isDragging) //Reduce speed when pushing/dragging an object
        {
            speed *= 0.4f; 
        }

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

    private void HandleClimbing()
    {
        if (_currentClimbable == null)
        {
            StopClimbing();
            return;
        }

        _currentClimbable.HandleClimbing(this);

        if (_jumpPressed)
        {
            _jumpPressed = false;
            StopClimbing();
            _verticalVelocity = 3f;
        }
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
        Ray ray = new Ray(cameraHolder.position, cameraHolder.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance))
        {
            InteractableObject item = hit.collider.GetComponentInParent<InteractableObject>();

            if (item == null || item is CarryableObject carryable && carryable.IsCarried)
            {
                return;
            }

            item.OnInteract(this);
        }
    }

    public void PickupItem(CarryableObject item)
    {
        if (_carriedObject != null && _storedObject != null)
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

        ItemSlot slot = _carriedObject == null ? ItemSlot.Carried : ItemSlot.Stored;
        item.PickedUpBy(netIdentity, slot);
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

        if (_carriedObject == null)
        {
            _carriedObject = item;
            item.transform.SetParent(itemAnchor, false);
        }
        else
        {
            _storedObject = item;
            item.transform.SetParent(storageAnchor, false);
        }

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

    [Command]
    private void CommandSwapItems()
    {
        RpcSwapItems();
    }

    [ClientRpc]
    private void RpcSwapItems()
    {
        CarryableObject temp = _carriedObject;
        _carriedObject = _storedObject;
        _storedObject = temp;

        if (_carriedObject != null)
        {
            _carriedObject.transform.SetParent(itemAnchor, false);
            _carriedObject.transform.localPosition = Vector3.zero;
            _carriedObject.transform.localRotation = Quaternion.identity;
        }

        if (_storedObject != null)
        {
            _storedObject.transform.SetParent(storageAnchor, false);
            _storedObject.transform.localPosition = Vector3.zero;
            _storedObject.transform.localRotation = Quaternion.identity;
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody rigidbody = hit.collider.attachedRigidbody;

        if (rigidbody == null)
        {
            if (isLocalPlayer && !isServer && hit.moveDirection.y >= -0.3f)
            {
                NetworkIdentity netId = hit.collider.GetComponentInParent<NetworkIdentity>();
                if (netId != null)
                {
                    Vector3 pushDir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
                    CommandApplyForceToSegment(netId, pushDir * pushForce);
                }
            }
            return;
        }

        if (rigidbody.isKinematic || hit.moveDirection.y < -0.3f)
        {
            return;
        }

        Vector3 dir = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        rigidbody.AddForce(dir * pushForce, ForceMode.Force);
    }
}
