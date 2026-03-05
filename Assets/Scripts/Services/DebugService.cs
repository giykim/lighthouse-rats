using UnityEngine;

public class DebugService : MonoBehaviour
{
    public static DebugService Instance { get; private set; }

    [SerializeField] private bool showGizmos;
    [SerializeField] private bool playerSuperspeed;
    [SerializeField] private float superspeedMultiplier = 5f;
    [SerializeField] private bool fastDialogue;
    [SerializeField] private float fastDialogueMultiplier = 5f;

    public static bool ShowGizmos => Instance != null && Instance.showGizmos;
    public static bool PlayerSuperspeed => Instance != null && Instance.playerSuperspeed;
    public static float SuperspeedMultiplier => Instance != null ? Instance.superspeedMultiplier : 5f;
    public static bool FastDialogue => Instance != null && Instance.fastDialogue;
    public static float FastDialogueMultiplier => Instance != null ? Instance.fastDialogueMultiplier : 5f;

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
