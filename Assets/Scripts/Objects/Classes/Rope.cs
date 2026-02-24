using UnityEngine;

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
        if (_ropeGenerator?.Segments == null)
        {
            return;
        }

        foreach (GameObject segment in _ropeGenerator.Segments)
        {
            if (segment == null)
            {
                continue;
            }

            Collider col = segment.GetComponent<Collider>();
            if (col != null)
            {
                Physics.IgnoreCollision(playerCollider, col, ignore);
            }
        }
    }

    public override void HandleClimbing(PlayerController player)
    {
        if (_ropeGenerator == null || _ropeGenerator.Segments == null)
        {
            return;
        }

        int closestIndex = GetClosestSegmentIndex(player.transform.position);
        GameObject closestSegment = _ropeGenerator.Segments[closestIndex];

        Rigidbody segmentRb = closestSegment.GetComponent<Rigidbody>();
        if (segmentRb != null)
        {
            segmentRb.AddForce(player.transform.forward * 0.5f, ForceMode.Force);
        }

        Vector3 segmentPos = closestSegment.transform.position;
        Vector3 horizontalDelta = new Vector3(
            segmentPos.x - player.transform.position.x,
            0f,
            segmentPos.z - player.transform.position.z
        );
        Vector3 verticalMove = Vector3.up * player.MoveInput.y * climbSpeed;
        player.CharacterController.Move(horizontalDelta + verticalMove * Time.deltaTime);

        if (topAnchor != null && player.transform.position.y >= topAnchor.position.y)
        {
            player.transform.position = topAnchor.position;
            player.StopClimbing();
        }

        if (bottomAnchor != null && player.transform.position.y <= bottomAnchor.position.y)
        {
            player.StopClimbing();
        }
    }

    private int GetClosestSegmentIndex(Vector3 position)
    {
        int closest = 0;
        float bestDist = float.MaxValue;

        for (int i = 0; i < _ropeGenerator.Segments.Length; i++)
        {
            if (_ropeGenerator.Segments[i] == null) continue;
            float dist = Vector3.Distance(position, _ropeGenerator.Segments[i].transform.position);
            if (dist < bestDist) { bestDist = dist; closest = i; }
        }

        return closest;
    }
}