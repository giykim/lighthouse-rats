using UnityEngine;

public class DebugService : MonoBehaviour
{
    public static DebugService Instance { get; private set; }

    [SerializeField] private bool showGizmos;
    [SerializeField] private bool playerSuperspeed;

    public static bool ShowGizmos => Instance != null && Instance.showGizmos;
    public static bool PlayerSuperspeed => Instance != null && Instance.playerSuperspeed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
