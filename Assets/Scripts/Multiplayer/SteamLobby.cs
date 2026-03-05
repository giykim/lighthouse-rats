using Mirror;
using UnityEngine;
using Steamworks;

public class SteamLobby : MonoBehaviour
{
    public static CSteamID CurrentLobbyId { get; private set; }

    public static void ClearLobbyId() => CurrentLobbyId = CSteamID.Nil;

    [SerializeField]
    private GameObject hostButton;

    private NetworkManager NetworkManager => NetworkManager.singleton;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HOST_ADDRESS_KEY = "HostAddress";

    private void Start()
    {
        if (!TransportSelector.SteamAvailable)
            return;

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby()
    {
        if (!TransportSelector.SteamAvailable)
        {
            NetworkManager.StartHost();
            return;
        }

        hostButton.SetActive(false);
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, NetworkManager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            hostButton.SetActive(true);
            return;
        }

        NetworkManager.StartHost();

        CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        SteamMatchmaking.SetLobbyData(CurrentLobbyId, HOST_ADDRESS_KEY, SteamUser.GetSteamID().ToString());
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        if (NetworkServer.active)
            return;

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HOST_ADDRESS_KEY);

        NetworkManager.networkAddress = hostAddress;
        NetworkManager.StartClient();

        CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        LobbyManager.NotifyLobbyReady();

        hostButton.SetActive(false);
    }
}
