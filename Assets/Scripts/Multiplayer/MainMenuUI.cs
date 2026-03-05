using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private SteamLobby steamLobby;

    private void Start()
    {
        continueButton.interactable = SaveLoadService.Instance != null && SaveLoadService.Instance.HasSave;
        settingsPanel.SetActive(false);
    }

    public void OnNewGame()
    {
        SaveLoadService.Instance.RequestNewGame();
        mainMenuPanel.SetActive(false);
        LobbyManager.NotifyLobbyReady();
        steamLobby.HostLobby();
    }

    public void OnContinue()
    {
        mainMenuPanel.SetActive(false);
        LobbyManager.NotifyLobbyReady();
        steamLobby.HostLobby();
    }

    public void OnJoin()
    {
        NetworkManager.singleton.StartClient();
        mainMenuPanel.SetActive(false);
        LobbyManager.NotifyLobbyReady();
    }

    public void OnSettings()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}
