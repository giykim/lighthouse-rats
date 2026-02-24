using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarryableObject : InteractableObject
{
    [Header("Item Info")]
    [SerializeField]
    private string itemName = "Item";
    [SerializeField]
    private Sprite itemIcon;

    [SyncVar]
    [SerializeField]
    private bool _isCarried;

    [SyncVar(hook = nameof(OnCarrierChanged))]
    private NetworkIdentity _carrier;

    public bool IsCarried => _isCarried;
    public string ItemName => itemName;
    public Sprite ItemIcon => itemIcon;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnInteract(PlayerController player)
    {
        if (IsCarried)
        {
            return;
        }
        player.PickupItem(this);
    }

    public override string GetPromptText()
    {
        return $"Press E to pick up {itemName}";
    }

    public void PickedUpBy(NetworkIdentity carrier)
    {
        _isCarried = true;
        _carrier = carrier;
    }

    public void Dropped()
    {
        _isCarried = false;
        _carrier = null;
    }

    private void OnCarrierChanged(NetworkIdentity oldCarrier, NetworkIdentity newCarrier)
    {
        if (newCarrier == null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.detectCollisions = true;
            transform.SetParent(null);
            return;
        }

        PlayerController carrier = newCarrier.GetComponent<PlayerController>();
        if (carrier != null)
        {
            _rigidbody.isKinematic = true;
            _rigidbody.detectCollisions = false;
            transform.SetParent(carrier.ItemHolder, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    public virtual void OnUse(PlayerController user) { }
}
