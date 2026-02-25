using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
[RequireComponent(typeof(NetworkTransformReliable))]
public abstract class InteractableObject : NetworkBehaviour {
    public abstract void OnInteract(PlayerController player);
    public virtual string GetPromptText() { return "Press E to interact"; }

}
