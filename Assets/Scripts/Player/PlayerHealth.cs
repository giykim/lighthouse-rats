using Mirror;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar]
    private bool _isAlive = true;

    public bool IsAlive => _isAlive;

    public override void OnStartServer()
    {
        GameClock.OnServerDayEnd += OnDayEnd;
    }

    public override void OnStopServer()
    {
        GameClock.OnServerDayEnd -= OnDayEnd;
    }

    private void OnDayEnd()
    {
        if (_isAlive)
        {
            Die();
        }
    }

    [Server]
    public void Die()
    {
        _isAlive = false;
    }
}
