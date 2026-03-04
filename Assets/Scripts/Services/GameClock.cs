using Mirror;
using UnityEngine;

public class GameClock : NetworkBehaviour
{
    public static GameClock Instance { get; private set; }

    private const float HOURS_IN_DAY = 24f;

    [Header("Time Settings")]
    [SerializeField]
    private float dayLengthSeconds = 300f;
    [SerializeField]
    private float startHour = 6f;

    [SyncVar]
    private float _currentHour;

    public float CurrentHour => _currentHour;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnStartServer()
    {
        _currentHour = startHour;
    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }

        float hoursPerSecond = HOURS_IN_DAY / dayLengthSeconds;
        _currentHour = (_currentHour + hoursPerSecond * Time.deltaTime) % HOURS_IN_DAY;
    }
}
