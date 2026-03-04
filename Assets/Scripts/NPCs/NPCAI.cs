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
    private float detectionBuildTime = 3f;
    [SerializeField]
    private float detectionDecayTime = 5f;
    [SerializeField]
    private float eventStateDuration = 15f;

    [Header("Speed")]
    [SerializeField]
    private float patrolSpeed = 2f;
    [SerializeField]
    private float chaseSpeed = 4f;

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
        float nearestSqr = detectionRadius * detectionRadius;
        Transform found = null;

        foreach (var p in PlayerRegistry.All)
        {
            float distSqr = (p.position - transform.position).sqrMagnitude;
            if (distSqr < nearestSqr)
            {
                nearestSqr = distSqr;
                found = p;
            }
        }

        if (found != null)
        {
            _detectionProgress = Mathf.Min(1f, _detectionProgress + Time.deltaTime / detectionBuildTime);

            if (_detectionProgress >= 1f)
            {
                _target = found;
                if (_state != NPCState.Chase)
                    EnterEventState(NPCState.Chase);
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
        if (schedule == null) return null;
        foreach (var entry in schedule)
        {
            if (hour >= entry.startHour && hour < entry.endHour)
                return entry;
        }
        return null;
    }

    private static NPCState BehaviorToState(NPCBehavior behavior) => behavior switch
    {
        NPCBehavior.Patrol      => NPCState.Patrol,
        NPCBehavior.Sleep       => NPCState.Sleep,
        NPCBehavior.Investigate => NPCState.Investigate,
        _                         => NPCState.Idle,
    };

    private void ExecuteIdle()
    {
        _agent.speed = patrolSpeed;
        // Walk to first waypoint if one is set, then stop
        if (_currentEntry?.waypoints?.Length > 0 && !_agent.hasPath && !_agent.pathPending)
            _agent.SetDestination(_currentEntry.waypoints[0].position);
    }

    private void ExecutePatrol()
    {
        Transform[] waypoints = _currentEntry?.waypoints;
        if (waypoints == null || waypoints.Length == 0) return;

        _agent.speed = patrolSpeed;
        if (_agent.pathPending) return;

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
        // Walk to sleep waypoint if not already there, then stop
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

        _agent.speed = chaseSpeed;
        _agent.SetDestination(_target.position);
    }

    private void ExecuteInvestigate()
    {
        _agent.speed = patrolSpeed;
        if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
        {
            Vector3 randomPoint = transform.position + Random.insideUnitSphere * 5f;
            randomPoint.y = transform.position.y;
            if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                _agent.SetDestination(hit.position);
        }
    }
}
