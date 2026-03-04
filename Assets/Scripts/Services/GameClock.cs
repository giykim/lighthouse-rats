using Mirror;
using System;
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

    [SyncVar]
    private int _currentDay = 1;

    public float CurrentHour => _currentHour;
    public int CurrentDay => _currentDay;

    public static event Action<int> OnDayChanged;
    public static event Action OnServerDayEnd;
    public static event Action OnServerNewDay;

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
        _currentDay = 1;
    }

    private void Update()
    {
        if (!isServer)
        {
            return;
        }

        float hoursPerSecond = HOURS_IN_DAY / dayLengthSeconds;
        _currentHour += hoursPerSecond * Time.deltaTime;

        if (_currentHour >= HOURS_IN_DAY)
        {
            OnServerDayEnd?.Invoke();
            _currentHour = startHour;
            _currentDay++;
            RpcOnDayChanged(_currentDay);
            OnServerNewDay?.Invoke();
        }
    }

    [Server]
    public void AdvanceToNextDay()
    {
        _currentHour = startHour;
        _currentDay++;
        RpcOnDayChanged(_currentDay);
        OnServerNewDay?.Invoke();
    }

    [ClientRpc]
    private void RpcOnDayChanged(int newDay)
    {
        OnDayChanged?.Invoke(newDay);
    }
}
