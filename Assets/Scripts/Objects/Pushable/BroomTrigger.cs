using System.Diagnostics;
using UnityEngine;

public class BroomTrigger : MonoBehaviour
{
    [SerializeField] private Rigidbody broomRigidbody;
    [SerializeField] private bool move_frozen = true;

    private int playersInside = 0;

    private void OnTriggerEnter(Collider other)
    {
        UnityEngine.Debug.Log("Player entered broom trigger");
            playersInside++;
            UpdateConstraints();
    }

    private void OnTriggerExit(Collider other)
    {

            playersInside--;
            UpdateConstraints();
    }

    private void UpdateConstraints()
    {
        if (playersInside > 0)
        {
            // Allow movement
            broomRigidbody.isKinematic = false;
            move_frozen = false;
        }
    }
}