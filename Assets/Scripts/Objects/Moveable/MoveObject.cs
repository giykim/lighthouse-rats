using UnityEngine;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransformReliable))]
public class MoveObject : InteractableObject
{
    [Header("Drag Settings")]
    [SerializeField] private float dragDistance = 1.5f;

    [SyncVar]
    private bool _isDragging;

    [SyncVar]
    private NetworkIdentity _draggingPlayerIdentity;

    private Rigidbody _rigidbody;
    private Vector3 _dragOffset;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        // Freeze Y position and all rotations to keep it on ground and stable
        _rigidbody.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
        _rigidbody.isKinematic = true; // Prevent movement when not dragging
    }

    public override void OnInteract(PlayerController player)
    {
        if (_isDragging)
        {
            player.SetDragging(false);
            CommandStopDragging();
        }
        else
        {
            player.SetDragging(true);
            CommandStartDragging(player.netIdentity);
        }
    }

    public override string GetPromptText(PlayerController player)
    {
        return _isDragging ? "Press E to stop dragging" : "Press E to drag";
    }

    [Command(requiresAuthority = false)]
    private void CommandStartDragging(NetworkIdentity playerIdentity)
    {
        _isDragging = true;
        _draggingPlayerIdentity = playerIdentity;
        PlayerController player = playerIdentity.GetComponent<PlayerController>();
        float xSize = transform.localScale.x;
        float zSize = transform.localScale.z;
        float safeDistance = Mathf.Max(xSize, zSize); // half the size in that axis
        _dragOffset = player.transform.forward * safeDistance;  
                _rigidbody.isKinematic = false;
                RpcStartDragging();
    }

    [Command(requiresAuthority = false)]
    private void CommandStopDragging()
    {
        _isDragging = false;
        _draggingPlayerIdentity = null;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
        RpcStopDragging();
    }

    [ClientRpc]
    private void RpcStartDragging()
    {
        _rigidbody.isKinematic = false;
    }

    [ClientRpc]
    private void RpcStopDragging()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.isKinematic = true;
    }

    private void Update()
    {
        if (_isDragging && _draggingPlayerIdentity != null)
        {
            PlayerController player = _draggingPlayerIdentity.GetComponent<PlayerController>();
            if (player != null)
            {
                Vector3 targetPosition = player.transform.position + _dragOffset;
                targetPosition.y = transform.position.y; // Keep original Y
                transform.position = targetPosition;
            }
        }
    }
}
