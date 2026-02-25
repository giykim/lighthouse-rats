using UnityEngine;
using Mirror;

[RequireComponent(typeof(RopeGenerator))]
public class Rope : ClimbableObject
{
    private RopeGenerator _ropeGenerator;

    private void Awake()
    {
        _ropeGenerator = GetComponent<RopeGenerator>();
    }

    public override string GetPromptText() => "Press E to climb rope";

    public override void OnInteract(PlayerController player)
    {
        SetSegmentCollisionsIgnored(player.CharacterController, true);
        base.OnInteract(player);
    }

    public void SetSegmentCollisionsIgnored(Collider playerCollider, bool ignore)
    {
        if (_ropeGenerator?.Segments == null) return;

        foreach (GameObject segment in _ropeGenerator.Segments)
        {
            if (segment == null) continue;
            Collider col = segment.GetComponent<Collider>();
            if (col != null)
                Physics.IgnoreCollision(playerCollider, col, ignore);
        }
    }

    public override void HandleClimbing(PlayerController player)
    {
        GameObject[] segments = _ropeGenerator?.Segments;
        if (segments == null) return;

        Vector3 anchorPos = player.ItemAnchor.position;

        int closestIndex = GetClosestSegmentIndex(anchorPos, segments);
        GameObject closestSegment = segments[closestIndex];
        Vector3 segmentPos = closestSegment.transform.position;

        if (isServer)
        {
            Rigidbody rb = closestSegment.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(player.transform.forward * 0.5f, ForceMode.Force);
            }
        }
        else
        {
            NetworkIdentity netId = closestSegment.GetComponent<NetworkIdentity>();
            if (netId != null)
            {
                player.CommandApplyForceToSegment(netId, player.transform.forward * 0.5f);
            }
        }

        Vector3 ropeAxis = (topAnchor != null && bottomAnchor != null)
            ? (bottomAnchor.position - topAnchor.position).normalized
            : Vector3.up;

        Vector3 fullDelta = segmentPos - anchorPos;
        Vector3 perpendicularSnap = fullDelta - Vector3.Dot(fullDelta, ropeAxis) * ropeAxis;

        Vector3 playerFacing = player.GetComponentInChildren<Camera>().transform.forward;

        Vector3 toPrev = closestIndex > 0
            ? (segments[closestIndex - 1].transform.position - anchorPos).normalized
            : Vector3.zero;
        Vector3 toNext = closestIndex < segments.Length - 1
            ? (segments[closestIndex + 1].transform.position - anchorPos).normalized
            : Vector3.zero;

        bool hasPrev = toPrev != Vector3.zero;
        bool hasNext = toNext != Vector3.zero;

        Vector3 forwardDir;
        if (hasPrev && hasNext)
            forwardDir = Vector3.Dot(playerFacing, toNext) >= Vector3.Dot(playerFacing, toPrev) ? toNext : toPrev;
        else if (hasNext)
            forwardDir = toNext;
        else if (hasPrev)
            forwardDir = toPrev;
        else
            forwardDir = Vector3.zero;

        float moveInput = player.MoveInput.y;
        if (!hasPrev || !hasNext)
            moveInput = Mathf.Max(0f, moveInput);

        player.CharacterController.Move(perpendicularSnap + forwardDir * moveInput * climbSpeed * Time.deltaTime);

    }

    private int GetClosestSegmentIndex(Vector3 position, GameObject[] segments)
    {
        int closest = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i] == null) continue;
            float dist = Vector3.Distance(position, segments[i].transform.position);
            if (dist < bestDist) { bestDist = dist; closest = i; }
        }

        return closest;
    }
}
