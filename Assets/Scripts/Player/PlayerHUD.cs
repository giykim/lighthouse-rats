using Mirror;
using TMPro;
using UnityEngine;

public class PlayerHUD : NetworkBehaviour
{
    [SerializeField]
    private GameObject hudCanvas;
    [SerializeField]
    private TextMeshProUGUI clockText;
    [SerializeField]
    private TextMeshProUGUI dateText;
    [SerializeField]
    private TextMeshProUGUI interactText;

    private void Awake()
    {
        hudCanvas.SetActive(false);
    }

    public override void OnStartLocalPlayer()
    {
        LobbyManager.OnGameStarted += OnGameStarted;
        if (GameClock.Instance != null && GameClock.Instance.IsRunning)
            hudCanvas.SetActive(true);
    }

    public override void OnStopLocalPlayer()
    {
        LobbyManager.OnGameStarted -= OnGameStarted;
    }

    private void OnGameStarted()
    {
        LobbyManager.OnGameStarted -= OnGameStarted;
        hudCanvas.SetActive(true);
    }

    private void Update()
    {
        if (!isLocalPlayer || GameClock.Instance == null)
        {
            return;
        }

        float hour = GameClock.Instance.CurrentHour;
        int h = Mathf.FloorToInt(hour) % 24;
        int m = Mathf.FloorToInt((hour % 1f) * 60f);
        clockText.text = $"{h:00}:{m:00}";

        int day = GameClock.Instance.CurrentDay;
        dateText.text = $"Day {day}";

        UpdateInteractPrompt();
    }

    private void UpdateInteractPrompt()
    {
        if (interactText == null)
        {
            return;
        }

        InteractableObject item = GetComponent<PlayerController>().GetLookedAtInteractable();
        interactText.text = item != null ? item.GetPromptText() : string.Empty;
    }
}
