using UnityEngine;

public class DebugService : MonoBehaviour
{
    public static DebugService Instance { get; private set; }

    [SerializeField] private bool showGizmos;
    [SerializeField] private bool playerSuperspeed;
    [SerializeField] private bool fastDialogue;

    public static bool ShowGizmos => Instance != null && Instance.showGizmos;
    public static bool PlayerSuperspeed => Instance != null && Instance.playerSuperspeed;
    public static bool FastDialogue => Instance != null && Instance.fastDialogue;

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
