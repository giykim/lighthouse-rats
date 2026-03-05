using Mirror;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PushableObject : InteractableObject
{
    [Header("Push Settings")]
    [SerializeField] private float pushForce = 5f;
    [SerializeField] private float pushCooldown = 0.5f;

    private Rigidbody _rigidbody;
    private float _lastPushTime;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public override void OnInteract(PlayerController player)
    {
        CommandPush(player.transform.forward);
    }

    public override string GetPromptText(PlayerController player)
    {
        return "Press E to push";
    }

    [Command(requiresAuthority = false)]
    private void CommandPush(Vector3 direction)
    {
        if (Time.time - _lastPushTime < pushCooldown)
        {
            return;
        }

        _lastPushTime = Time.time;
        RpcApplyForce(direction);
    }

    [ClientRpc]
    private void RpcApplyForce(Vector3 direction)
    {
        _rigidbody.AddForce(direction * pushForce, ForceMode.Impulse);
    }
}
