using Mirror;
using UnityEngine;

public class SlidableBox : InteractableObject
{
    [SerializeField] private string progressKey;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private Transform pushSpot;
    [SerializeField] private float pushSpotRadius = 1.5f;

    [SyncVar(hook = nameof(OnPushedChanged))]
    private bool _pushed;

    private int _waypointIndex;
    private bool _moving;

    public override void OnStartServer()
    {
        if (GameProgress.Instance != null && GameProgress.Instance.Has(progressKey))
        {
            _pushed = true;
            if (waypoints != null && waypoints.Length > 0)
                transform.position = waypoints[waypoints.Length - 1].position;
        }
    }

    public override void OnStartClient()
    {
        if (_pushed && waypoints != null && waypoints.Length > 0)
            transform.position = waypoints[waypoints.Length - 1].position;
    }

    private void OnPushedChanged(bool _, bool newVal) { }

    public override void OnInteract(PlayerController player)
    {
        if (!_pushed && IsPlayerInPushSpot(player))
            CommandPush();
    }

    private bool IsPlayerInPushSpot(PlayerController player)
    {
        if (pushSpot == null)
            return true;

        return Vector3.Distance(player.transform.position, pushSpot.position) <= pushSpotRadius;
    }

    [Command(requiresAuthority = false)]
    private void CommandPush()
    {
        if (_pushed)
            return;

        _pushed = true;
        GameProgress.Instance?.Complete(progressKey);
        _moving = true;
    }

    private void Update()
    {
        if (!isServer || !_moving)
            return;

        if (_waypointIndex >= waypoints.Length)
        {
            _moving = false;
            return;
        }

        Vector3 target = waypoints[_waypointIndex].position;
        transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < 0.01f)
        {
            transform.position = target;
            _waypointIndex++;
        }
    }

    public override string GetPromptText(PlayerController player)
    {
        if (_pushed)
            return "";
        if (pushSpot != null && !IsPlayerInPushSpot(player))
            return "Can't push from here";
        return "Press E to push";
    }

    private void OnDrawGizmos()
    {
        if (!DebugService.ShowGizmos)
            return;

        // Push spot
        if (pushSpot != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pushSpot.position, pushSpotRadius);
        }

        // Waypoint path
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, waypoints[0].position);

        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null)
                continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.15f);

            if (i + 1 < waypoints.Length && waypoints[i + 1] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
        }
    }
}
