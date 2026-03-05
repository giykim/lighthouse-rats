using System.IO;
using UnityEngine;

public class SaveLoadService : MonoBehaviour
{
    public static SaveLoadService Instance { get; private set; }

    private const string SaveFileName = "save.json";
    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    public SaveData CurrentSave { get; private set; }
    public bool NewGameRequested { get; private set; }
    public bool HasSave => CurrentSave != null && !NewGameRequested;

    public void RequestNewGame() => NewGameRequested = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        Load();
    }

    private void OnEnable()
    {
        GameClock.OnServerDayEnd += SaveGame;
    }

    private void OnDisable()
    {
        GameClock.OnServerDayEnd -= SaveGame;
    }

    private void Load()
    {
        if (!File.Exists(SavePath))
        {
            CurrentSave = null;
            return;
        }

        string json = File.ReadAllText(SavePath);
        CurrentSave = JsonUtility.FromJson<SaveData>(json);
        Debug.Log($"[SaveLoadService] Loaded save: Day {CurrentSave.currentDay}");
    }

    public void SaveGame()
    {
        if (GameClock.Instance == null || GameProgress.Instance == null)
            return;

        var data = new SaveData
        {
            currentDay = GameClock.Instance.CurrentDay,
            completedEvents = GameProgress.Instance.GetCompletedEvents()
        };

        NewGameRequested = false;
        CurrentSave = data;
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, prettyPrint: true));
        Debug.Log($"[SaveLoadService] Saved game: Day {data.currentDay}");
    }

    public void DeleteSave()
    {
        CurrentSave = null;
        if (File.Exists(SavePath))
            File.Delete(SavePath);
        Debug.Log("[SaveLoadService] Save deleted.");
    }
}
