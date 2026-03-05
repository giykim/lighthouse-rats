using Mirror;
using UnityEngine;

[RequireComponent(typeof(NetworkIdentity))]
public abstract class InteractableObject : NetworkBehaviour {
    public abstract void OnInteract(PlayerController player);
    public virtual string GetPromptText(PlayerController player) { return "Press E to interact"; }

}
