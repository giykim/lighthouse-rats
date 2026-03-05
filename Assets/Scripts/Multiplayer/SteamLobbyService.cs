using Mirror;
using Steamworks;
using UnityEngine;

public class SteamLobbyService : MonoBehaviour
{
    private Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    private Callback<LobbyEnter_t> _lobbyEntered;

    private void Start()
    {
        if (!TransportSelector.SteamAvailable)
        {
            enabled = false;
            return;
        }

        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(_ => RefreshNames());
        _lobbyEntered = Callback<LobbyEnter_t>.Create(_ => RefreshNames());
        NetworkClient.OnDisconnectedEvent += OnDisconnected;
    }

    private void OnDestroy()
    {
        if (TransportSelector.SteamAvailable)
            NetworkClient.OnDisconnectedEvent -= OnDisconnected;
    }

    private void OnDisconnected()
    {
        CSteamID lobbyId = SteamLobby.CurrentLobbyId;
        SteamLobby.ClearLobbyId();
        if (lobbyId != CSteamID.Nil)
            SteamMatchmaking.LeaveLobby(lobbyId);
    }

    private void RefreshNames()
    {
        CSteamID lobbyId = SteamLobby.CurrentLobbyId;
        if (lobbyId == CSteamID.Nil)
            return;

        int count = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        string[] names = new string[count];
        for (int i = 0; i < count; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
            names[i] = SteamFriends.GetFriendPersonaName(memberId);
        }
        LobbyManager.UpdatePlayerNames(names);
    }
}
