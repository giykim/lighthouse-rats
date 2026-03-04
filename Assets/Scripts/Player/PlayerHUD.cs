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

    public override void OnStartLocalPlayer()
    {
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
    }
}
