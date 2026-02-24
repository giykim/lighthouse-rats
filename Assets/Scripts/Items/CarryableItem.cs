using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarryableItem : NetworkBehaviour
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
        if (newCarrier == null) return;

        PlayerController carrier = newCarrier.GetComponent<PlayerController>();
        if (carrier != null)
        {
            transform.SetParent(carrier.ItemHolder, false);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }

    public virtual void OnUse(PlayerController user) { }
}
