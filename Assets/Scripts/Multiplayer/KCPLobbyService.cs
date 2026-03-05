using Mirror;
using UnityEngine;

public class KCPLobbyService : MonoBehaviour
{
    private int _lastConnectionCount = -1;

    private void OnEnable()
    {
        if (TransportSelector.SteamAvailable)
            return;

        LobbyManager.OnLobbyReady += OnLobbyReady;
    }

    private void OnDisable()
    {
        LobbyManager.OnLobbyReady -= OnLobbyReady;
    }

    private void OnLobbyReady()
    {
        LobbyManager.UpdatePlayerNames(new[] { "Guest 1" });
    }

    private void Update()
    {
        if (!NetworkServer.active || TransportSelector.SteamAvailable)
            return;

        int count = NetworkServer.connections.Count;
        if (count == _lastConnectionCount)
            return;

        _lastConnectionCount = count;

        string[] names = new string[count];
        for (int i = 0; i < count; i++)
            names[i] = $"Guest {i + 1}";

        LobbyManager.UpdatePlayerNames(names);
    }
}
