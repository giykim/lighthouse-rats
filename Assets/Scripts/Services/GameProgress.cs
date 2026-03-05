using Mirror;
using System;

public class GameProgress : NetworkBehaviour
{
    public static GameProgress Instance { get; private set; }

    private readonly SyncList<string> _completedEvents = new();

    public static event Action<string> OnEventCompleted;
    public static event Action<string> OnServerEventCompleted;

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
        if (SaveLoadService.Instance != null && SaveLoadService.Instance.HasSave)
        {
            foreach (string ev in SaveLoadService.Instance.CurrentSave.completedEvents)
                _completedEvents.Add(ev);
        }
    }

    public override void OnStartClient()
    {
        _completedEvents.OnAdd += index => OnEventCompleted?.Invoke(_completedEvents[index]);
    }

    [Server]
    public void Complete(string eventKey)
    {
        if (!_completedEvents.Contains(eventKey))
        {
            _completedEvents.Add(eventKey);
            OnServerEventCompleted?.Invoke(eventKey);
        }
    }
    
    public bool Has(string eventKey) => _completedEvents.Contains(eventKey);

    public string[] GetCompletedEvents()
    {
        string[] result = new string[_completedEvents.Count];
        for (int i = 0; i < _completedEvents.Count; i++)
            result[i] = _completedEvents[i];
        return result;
    }
}
