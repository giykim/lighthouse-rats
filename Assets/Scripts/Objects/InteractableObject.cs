using Mirror;
using UnityEngine;

public abstract class InteractableObject : NetworkBehaviour {
    public abstract void OnInteract(PlayerController player);
    public virtual string GetPromptText() { return "Press E to interact"; }

}
