using Mirror;
using UnityEngine;
using UnityEngine.AI;

public class ResetObject : NetworkBehaviour
{
    [SerializeField]
    private bool resetPosition = true;
    [SerializeField]
    private bool resetRotation = true;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private NavMeshAgent _navMeshAgent;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
        _navMeshAgent = GetComponent<NavMeshAgent>();
        _rigidbody = GetComponent<Rigidbody>();
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
        if (resetPosition)
        {
            if (_navMeshAgent != null && _navMeshAgent.enabled)
            {
                _navMeshAgent.Warp(_spawnPosition);
            }
            else
            {
                transform.position = _spawnPosition;
            }
        }

        if (resetRotation)
        {
            transform.rotation = _spawnRotation;
        }

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }
    }
}
