using UnityEngine;
using Mirror;

public class Door : ToggleableObject
{
    [Header("Door Settings")]
    [SerializeField]
    private float openAngle = 90f;
    [SerializeField]
    private float openSpeed = 3f;
    [SerializeField]
    private bool lockedByDefault = false;

    [SyncVar(hook = nameof(OnIsLockedChanged))]
    private bool _isLocked = false;

    private Quaternion _closedRotation;
    private Quaternion _openRotation;
    private bool _isAnimating = false;

    private void Awake()
    {
        _closedRotation = transform.localRotation;
        _openRotation = Quaternion.Euler(
            transform.localEulerAngles + new Vector3(0f, openAngle, 0f));
        _isLocked = lockedByDefault;
    }

    protected override bool CanToggle(PlayerController player)
    {
        return !_isLocked;
    }

    protected override void OnStateChanged(bool isOn)
    {
        _isAnimating = true;
    }

    public override string GetPromptText()
    {
        if (_isLocked)
        {
            return "Locked";
        }

        return IsToggled ? "Press E to close" : "Press E to open";
    }

    private void Update()
    {
        if (!_isAnimating)
        {
            return;
        }

        Quaternion target = IsToggled ? _openRotation : _closedRotation;
        transform.localRotation = Quaternion.Lerp(
            transform.localRotation, target, Time.deltaTime * openSpeed);

        if (Quaternion.Angle(transform.localRotation, target) < 0.1f)
        {
            transform.localRotation = target;
            _isAnimating = false;
        }
    }

    private void OnIsLockedChanged(bool oldVal, bool newVal) { }

    [Server]
    public void Unlock() => _isLocked = false;

    [Server]
    public void Lock() => _isLocked = true;

    public bool IsLocked => _isLocked;
}