using Mirror;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCAI : NetworkBehaviour
{
    public enum NPCState { Idle, Patrol, Sleep, Investigate, Chase }

    [Header("Schedule")]
    [SerializeField]
    private ScheduleEntry[] schedule;
    [SerializeField]
    private float waypointWaitTime = 2f;

    [Header("Detection")]
    [SerializeField]
    private float detectionRadius = 8f;
    [SerializeField]
    private float coneDistance = 12f;
    [SerializeField]
    [Range(1f, 360f)]
    private float detectionAngle = 90f;
    [SerializeField]
    private float detectionBuildTime = 3f;
    [SerializeField]
    private float detectionDecayTime = 5f;
    [SerializeField]
    private float eventStateDuration = 15f;
    [SerializeField]
    private float eyeHeight = 1.6f;

    [Header("Speed")]
    [SerializeField]
    private float patrolSpeed = 2f;
    [SerializeField]
    private float chaseSpeed = 4f;

    [Header("Attack")]
    [SerializeField]
    private float attackRange = 1.5f;

    [SyncVar]
    private NPCState _state = NPCState.Idle;

    [SyncVar]
    private float _detectionProgress;

    private NavMeshAgent _agent;
    private int _waypointIndex;
    private float _waypointTimer;
    private float _eventTimer;
    private Transform _target;
    private ScheduleEntry _currentEntry;

    public NPCState State => _state;
    public float DetectionProgress => _detectionProgress;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    public override void OnStartServer()
    {
        GameClock.OnServerNewDay += OnNewDay;
    }

    public override void OnStopServer()
    {
        GameClock.OnServerNewDay -= OnNewDay;
    }

    private void OnNewDay()
    {
        _target = null;
        _state = NPCState.Idle;
        _detectionProgress = 0f;
        _eventTimer = 0f;
        _agent.ResetPath();
    }

    public override void OnStartClient()
    {
        if (!isServer)
        {
            _agent.enabled = false;
        }
    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }

        UpdateDetection();
        UpdateState();
    }

    private void UpdateDetection()
    {
        float maxRangeSqr = Mathf.Max(detectionRadius, coneDistance);
        maxRangeSqr *= maxRangeSqr;
        float nearestSqr = maxRangeSqr;
        Transform found = null;
        float foundDist = 0f;
        float foundMaxDist = 1f;

        foreach (var p in PlayerRegistry.All)
        {
            PlayerHealth health = p.GetComponent<PlayerHealth>();
            if (health != null && !health.IsAlive)
            {
                continue;
            }

            float distSqr = (p.position - transform.position).sqrMagnitude;
            if (distSqr >= nearestSqr)
            {
                continue;
            }

            bool inSphere = distSqr < detectionRadius * detectionRadius;
            bool inCone = distSqr < coneDistance * coneDistance &&
                          Vector3.Angle(transform.forward, (p.position - transform.position).normalized) < detectionAngle / 2f;

            if (!inSphere && !inCone)
            {
                continue;
            }

            Vector3 eye = transform.position + Vector3.up * eyeHeight;
            Vector3 toPlayer = p.position - eye;
            if (Physics.Raycast(eye, toPlayer.normalized, out RaycastHit hit, toPlayer.magnitude)
                && hit.transform.root != p.root && hit.transform.root != transform.root)
            {
                continue;
            }

            nearestSqr = distSqr;
            found = p;
            foundDist = Mathf.Sqrt(distSqr);
            foundMaxDist = inCone ? coneDistance : detectionRadius;
        }

        if (found != null)
        {
            float proximity = 1f - Mathf.Clamp01(foundDist / foundMaxDist);
            float rate = Mathf.Lerp(1f, 3f, proximity * proximity) / detectionBuildTime;
            _detectionProgress = Mathf.Min(1f, _detectionProgress + rate * Time.deltaTime);

            if (_detectionProgress >= 1f)
            {
                _target = found;
                if (_state != NPCState.Chase)
                {
                    EnterEventState(NPCState.Chase);
                }
            }
        }
        else
        {
            _detectionProgress = Mathf.Max(0f, _detectionProgress - Time.deltaTime / detectionDecayTime);
        }
    }

    private void EnterEventState(NPCState eventState)
    {
        _state = eventState;
        _eventTimer = eventStateDuration;
    }

    private void UpdateState()
    {
        if (_state == NPCState.Chase || _state == NPCState.Investigate)
        {
            _eventTimer -= Time.deltaTime;
            if (_eventTimer <= 0f)
            {
                _target = null;
                _state = NPCState.Idle;
            }
        }

        if (_state != NPCState.Chase && _state != NPCState.Investigate)
        {
            if (GameClock.Instance != null && schedule != null)
            {
                ScheduleEntry entry = GetEntryForHour(GameClock.Instance.CurrentHour);
                if (entry != _currentEntry)
                {
                    _currentEntry = entry;
                    _waypointIndex = 0;
                    _waypointTimer = 0f;
                }
                _state = entry != null ? BehaviorToState(entry.behavior) : NPCState.Idle;
            }
        }

        switch (_state)
        {
            case NPCState.Idle:
                ExecuteIdle();
                break;
            case NPCState.Patrol:
                ExecutePatrol();
                break;
            case NPCState.Sleep:
                ExecuteSleep();
                break;
            case NPCState.Chase:
                ExecuteChase();
                break;
            case NPCState.Investigate:
                ExecuteInvestigate();
                break;
        }
    }

    private ScheduleEntry GetEntryForHour(float hour)
    {
        if (schedule == null)
        {
            return null;
        }

        foreach (var entry in schedule)
        {
            if (hour >= entry.startHour && hour < entry.endHour)
            {
                return entry;
            }
        }

        return null;
    }

    private static NPCState BehaviorToState(NPCBehavior behavior) => behavior switch
    {
        NPCBehavior.Patrol      => NPCState.Patrol,
        NPCBehavior.Sleep       => NPCState.Sleep,
        NPCBehavior.Investigate => NPCState.Investigate,
        _                       => NPCState.Idle,
    };

    private void ExecuteIdle()
    {
        _agent.speed = patrolSpeed;
        if (_currentEntry?.waypoints?.Length > 0 && !_agent.hasPath && !_agent.pathPending)
        {
            _agent.SetDestination(_currentEntry.waypoints[0].position);
        }
    }

    private void ExecutePatrol()
    {
        Transform[] waypoints = _currentEntry?.waypoints;
        if (waypoints == null || waypoints.Length == 0)
        {
            return;
        }

        _agent.speed = patrolSpeed;
        if (_agent.pathPending)
        {
            return;
        }

        if (_agent.remainingDistance < 0.5f)
        {
            _waypointTimer -= Time.deltaTime;
            if (_waypointTimer <= 0f)
            {
                _agent.SetDestination(waypoints[_waypointIndex].position);
                _waypointTimer = waypointWaitTime;
                _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            }
        }
    }

    private void ExecuteSleep()
    {
        if (_currentEntry?.waypoints?.Length > 0 && !_agent.hasPath && !_agent.pathPending)
        {
            _agent.speed = patrolSpeed;
            _agent.SetDestination(_currentEntry.waypoints[0].position);
        }
        else if (_agent.remainingDistance < 0.5f)
        {
            _agent.ResetPath();
            _agent.speed = 0f;
        }
    }

    private void ExecuteChase()
    {
        if (_target == null)
        {
            return;
        }

        PlayerHealth targetHealth = _target.GetComponent<PlayerHealth>();
        if (targetHealth != null && !targetHealth.IsAlive)
        {
            _target = null;
            _state = NPCState.Idle;
            return;
        }

        _agent.speed = chaseSpeed;
        _agent.SetDestination(_target.position);

        if ((_target.position - transform.position).sqrMagnitude <= attackRange * attackRange)
        {
            PlayerHealth health = _target.GetComponent<PlayerHealth>();
            if (health != null && health.IsAlive)
            {
                health.Die();
            }
        }
    }

    private void ExecuteInvestigate()
    {
        _agent.speed = patrolSpeed;
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * 5f;
            randomPoint.y = transform.position.y;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                _agent.SetDestination(hit.position);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (DebugService.ShowGizmos)
        {
            DrawGizmos();
        }
    }

    private void DrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.Lerp(Color.white, Color.red, _detectionProgress);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        DrawDetectionCone(coneDistance);

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Gizmos.DrawSphere(eye, 0.08f);

        foreach (var p in PlayerRegistry.All)
        {
            Vector3 toPlayer = p.position - eye;
            bool blocked = Physics.Raycast(eye, toPlayer.normalized, out RaycastHit losHit, toPlayer.magnitude)
                           && losHit.transform.root != p.root && losHit.transform.root != transform.root;
            Gizmos.color = blocked ? Color.magenta : Color.yellow;
            Gizmos.DrawLine(eye, p.position);
        }

        if (_target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, _target.position);
        }

        if (schedule == null)
        {
            return;
        }

        foreach (var entry in schedule)
        {
            if (entry.waypoints == null || entry.waypoints.Length == 0)
            {
                continue;
            }

            Gizmos.color = Color.cyan;
            for (int i = 0; i < entry.waypoints.Length; i++)
            {
                if (entry.waypoints[i] == null)
                {
                    continue;
                }

                Gizmos.DrawSphere(entry.waypoints[i].position, 0.2f);

                if (entry.behavior == NPCBehavior.Patrol && i + 1 < entry.waypoints.Length && entry.waypoints[i + 1] != null)
                {
                    Gizmos.DrawLine(entry.waypoints[i].position, entry.waypoints[i + 1].position);
                }
            }

            if (entry.behavior == NPCBehavior.Patrol && entry.waypoints.Length > 1
                && entry.waypoints[0] != null && entry.waypoints[^1] != null)
            {
                Gizmos.DrawLine(entry.waypoints[^1].position, entry.waypoints[0].position);
            }
        }
    }

    private void DrawDetectionCone(float distance)
    {
        float halfAngle = Mathf.Clamp(detectionAngle / 2f, 0f, 89f);
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        Vector3 up = transform.up;

        Vector3 baseCenter = origin + forward * distance;
        float baseRadius = distance * Mathf.Tan(halfAngle * Mathf.Deg2Rad);

        int segments = 24;
        Vector3 prev = baseCenter + right * baseRadius;

        for (int i = 1; i <= segments; i++)
        {
            float angle = (float)i / segments * 360f * Mathf.Deg2Rad;
            Vector3 point = baseCenter + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * baseRadius;

            Gizmos.DrawLine(prev, point);

            if (i % 6 == 0)
            {
                Gizmos.DrawLine(origin, point);
            }

            prev = point;
        }

        Gizmos.DrawLine(origin, baseCenter + right * baseRadius);
        Gizmos.DrawLine(origin, baseCenter - right * baseRadius);
        Gizmos.DrawLine(origin, baseCenter + up * baseRadius);
        Gizmos.DrawLine(origin, baseCenter - up * baseRadius);
    }
}
