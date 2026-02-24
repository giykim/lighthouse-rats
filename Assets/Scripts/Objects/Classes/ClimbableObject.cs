using UnityEngine;

public abstract class ClimbableObject : InteractableObject
{
    [Header("Climbable Settings")]
    [SerializeField] protected float climbSpeed = 2f;
    [SerializeField] protected Transform topAnchor;
    [SerializeField] protected Transform bottomAnchor;

    public float ClimbSpeed => climbSpeed;
    public Transform TopAnchor => topAnchor;
    public Transform BottomAnchor => bottomAnchor;

    public override void OnInteract(PlayerController player)
    {
        player.StartClimbing(this);
    }

    public abstract void HandleClimbing(PlayerController player);
}
