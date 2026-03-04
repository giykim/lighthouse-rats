using Mirror;
using System;

public class GameProgress : NetworkBehaviour
{
    public static GameProgress Instance { get; private set; }

    private readonly SyncList<string> _completedEvents = new();

    public static event Action<string> OnEventCompleted;

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
        _completedEvents.OnAdd += index => OnEventCompleted?.Invoke(_completedEvents[index]);
    }

    [Server]
    public void Complete(string eventKey)
    {
        if (!_completedEvents.Contains(eventKey))
        {
            _completedEvents.Add(eventKey);
        }
    }
    
    public bool Has(string eventKey) => _completedEvents.Contains(eventKey);
}
