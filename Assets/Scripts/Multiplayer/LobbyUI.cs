using Mirror;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private GameObject lobbyPanel;
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Transform playerListContent;
    [SerializeField] private GameObject playerNamePrefab;
    [SerializeField] private Button startButton;

    private void Awake()
    {
        NetworkClient.OnDisconnectedEvent += OnDisconnected;
    }

    private void OnDestroy()
    {
        NetworkClient.OnDisconnectedEvent -= OnDisconnected;
    }

    private void OnEnable()
    {
        LobbyManager.OnLobbyReady += ShowLobby;
        LobbyManager.OnGameStarted += HideLobby;
        LobbyManager.OnPlayerNamesUpdated += RefreshPlayerList;
    }

    private void OnDisable()
    {
        LobbyManager.OnLobbyReady -= ShowLobby;
        LobbyManager.OnGameStarted -= HideLobby;
        LobbyManager.OnPlayerNamesUpdated -= RefreshPlayerList;
    }

    private void OnApplicationQuit()
    {
        if (NetworkServer.active)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.active)
            NetworkManager.singleton.StopClient();
    }

    private void OnDisconnected()
    {
        lobbyPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    private void ShowLobby()
    {
        lobbyPanel.SetActive(true);
        startButton.gameObject.SetActive(NetworkServer.active || !NetworkClient.active);
    }

    private void HideLobby()
    {
        lobbyPanel.SetActive(false);
    }

    private void RefreshPlayerList(string[] names)
    {
        foreach (Transform child in playerListContent)
            Destroy(child.gameObject);

        foreach (string playerName in names)
        {
            GameObject entry = Instantiate(playerNamePrefab, playerListContent);
            entry.GetComponentInChildren<TMP_Text>().text = playerName;
        }
    }

    public void OnStartGame()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.StartGame();
    }

    public void OnLeave()
    {
        if (NetworkServer.active && LobbyManager.Instance != null)
            LobbyManager.Instance.StartShutdown();
        else if (NetworkClient.active)
            NetworkManager.singleton.StopClient();

        lobbyPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
}
