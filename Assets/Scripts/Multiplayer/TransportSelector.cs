using Mirror;
using Steamworks;
using UnityEngine;

public class TransportSelector : MonoBehaviour
{
    public static bool SteamAvailable { get; private set; }

    [SerializeField] private Transport kcpTransport;
    [SerializeField] private Transport steamTransport;
    [SerializeField] private GameObject steamServices;

    private void Awake()
    {
        SteamAvailable = SteamAPI.IsSteamRunning();

        if (steamServices != null)
            steamServices.SetActive(SteamAvailable);

        kcpTransport.enabled = !SteamAvailable;
        steamTransport.enabled = SteamAvailable;
    }

    private void Start()
    {
        Transport selected = SteamAvailable ? steamTransport : kcpTransport;
        Transport.active = selected;
        NetworkManager.singleton.transport = selected;
    }
}
