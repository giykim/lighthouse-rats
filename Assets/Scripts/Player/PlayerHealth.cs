using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerHealth : NetworkBehaviour
{
    [SyncVar]
    private bool _isAlive = true;

    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    public bool IsAlive => _isAlive;

    public override void OnStartServer()
    {
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
        GameClock.OnServerDayEnd += OnDayEnd;
        GameClock.OnServerNewDay += OnNewDay;
    }

    public override void OnStopServer()
    {
        GameClock.OnServerDayEnd -= OnDayEnd;
        GameClock.OnServerNewDay -= OnNewDay;
    }

    private void OnDayEnd()
    {
        if (_isAlive)
        {
            Die(fromDayEnd: true);
        }
    }

    private void OnNewDay()
    {
        _isAlive = true;

        Transform spawnPoint = NetworkManager.singleton.GetStartPosition();
        Vector3 spawnPosition = spawnPoint != null ? spawnPoint.position : _spawnPosition;
        Quaternion spawnRotation = spawnPoint != null ? spawnPoint.rotation : _spawnRotation;

        TargetOnRespawned(connectionToClient, spawnPosition, spawnRotation);
    }

    [Server]
    public void Die(bool fromDayEnd = false)
    {
        _isAlive = false;
        TargetOnDied(connectionToClient);

        if (!fromDayEnd)
        {
            ServerCheckAllDead();
        }
    }

    [Server]
    private void ServerCheckAllDead()
    {
        foreach (var conn in NetworkServer.connections.Values)
        {
            if (conn.identity == null)
            {
                continue;
            }

            PlayerHealth health = conn.identity.GetComponent<PlayerHealth>();
            if (health != null && health.IsAlive)
            {
                return;
            }
        }

        GameClock.Instance.AdvanceToNextDay();
    }

    [TargetRpc]
    private void TargetOnDied(NetworkConnection conn)
    {
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            Camera cam = controller.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.enabled = false;
            }

            AudioListener listener = controller.GetComponentInChildren<AudioListener>();
            if (listener != null)
            {
                listener.enabled = false;
            }

            controller.enabled = false;
        }

        SpectatorCamera spectatorCamera = GetComponent<SpectatorCamera>();
        if (spectatorCamera != null)
        {
            spectatorCamera.Activate();
        }
    }

    [TargetRpc]
    private void TargetOnRespawned(NetworkConnection conn, Vector3 spawnPosition, Quaternion spawnRotation)
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
        }

        transform.position = spawnPosition;
        transform.rotation = spawnRotation;

        if (cc != null)
        {
            cc.enabled = true;
        }

        SpectatorCamera spectatorCamera = GetComponent<SpectatorCamera>();
        if (spectatorCamera != null)
        {
            spectatorCamera.Deactivate();
        }

        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null)
        {
            controller.enabled = true;

            Camera cam = controller.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.enabled = true;
            }

            AudioListener listener = controller.GetComponentInChildren<AudioListener>();
            if (listener != null)
            {
                listener.enabled = true;
            }
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
