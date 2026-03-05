using Mirror;
using System;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public static event Action OnLobbyReady;
    public static event Action OnGameStarted;
    public static event Action<string[]> OnPlayerNamesUpdated;

    [SyncVar(hook = nameof(OnGameStartedChanged))]
    private bool _gameStarted;

    [SyncVar(hook = nameof(OnServerClosingChanged))]
    private bool _serverClosing;

    private readonly SyncList<string> _playerNames = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnStartClient()
    {
        _playerNames.OnChange += OnNamesChanged;
        if (_playerNames.Count > 0)
            FireNamesUpdated();
    }

    public override void OnStopClient()
    {
        _playerNames.OnChange -= OnNamesChanged;
    }

    private void OnNamesChanged(SyncList<string>.Operation op, int index, string item)
    {
        FireNamesUpdated();
    }

    private void FireNamesUpdated()
    {
        string[] names = new string[_playerNames.Count];
        for (int i = 0; i < _playerNames.Count; i++)
            names[i] = _playerNames[i];
        OnPlayerNamesUpdated?.Invoke(names);
    }

    public static void NotifyLobbyReady() => OnLobbyReady?.Invoke();

    public static void UpdatePlayerNames(string[] names)
    {
        if (Instance != null && Instance.isServer)
            Instance.SetPlayerNames(names);
    }

    [Server]
    private void SetPlayerNames(string[] names)
    {
        _playerNames.Clear();
        foreach (string name in names)
            _playerNames.Add(name);
        FireNamesUpdated();
    }

    [Server]
    public void StartGame()
    {
        _gameStarted = true;
        if (GameClock.Instance != null)
            GameClock.Instance.StartClock();
    }

    [Server]
    public void StartShutdown()
    {
        _serverClosing = true;
        Invoke(nameof(DoShutdown), 0.5f);
    }

    [Server]
    private void DoShutdown()
    {
        NetworkManager.singleton.StopHost();
    }

    public override void OnStopServer()
    {
        _gameStarted = false;
        _serverClosing = false;
        _playerNames.Clear();
    }

    private void OnGameStartedChanged(bool _, bool newVal)
    {
        if (newVal)
            OnGameStarted?.Invoke();
    }

    private void OnServerClosingChanged(bool _, bool newVal)
    {
        if (newVal && !isServer)
            NetworkManager.singleton.StopClient();
    }
}
