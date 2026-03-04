using Mirror;

public abstract class ToggleableObject : InteractableObject
{
    [SyncVar(hook = nameof(OnToggleChanged))]
    private bool _isToggled = false;

    public bool IsToggled => _isToggled;

    public override void OnInteract(PlayerController player)
    {
        if (!CanToggle(player))
        {
            return;
        }

        CommandToggle();
    }

    [Command(requiresAuthority = false)]
    private void CommandToggle()
    {
        ServerPerformToggle();
    }

    [Server]
    protected void ServerPerformToggle()
    {
        _isToggled = !_isToggled;
    }

    private void OnToggleChanged(bool oldVal, bool newVal)
    {
        OnStateChanged(newVal);
    }

    protected virtual bool CanToggle(PlayerController player) => true;

    protected abstract void OnStateChanged(bool isOn);

    public override string GetPromptText()
    {
        return _isToggled ? "Press E to close" : "Press E to open";
    }
}
