using Mirror;
using System;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

public class GameEventManager : NetworkBehaviour
{
    [Serializable]
    private struct EventBinding
    {
        public string eventKey;
        public UnityEvent onTriggered;
    }

    [Serializable]
    private class SpawnBindingData
    {
        public string eventKey;
        public string prefabName;
        public string spawnPointName;
    }

    [Serializable]
    private class GameEventsConfig
    {
        public SpawnBindingData[] spawnBindings;
    }

    [SerializeField]
    private EventBinding[] bindings;

    private GameEventsConfig _config;

    public override void OnStartServer()
    {
        LoadConfig();
        GameProgress.OnServerEventCompleted += OnEventCompleted;
    }

    public override void OnStopServer()
    {
        GameProgress.OnServerEventCompleted -= OnEventCompleted;
    }

    private void LoadConfig()
    {
        string path = Path.Combine(Application.streamingAssetsPath, "GameEvents.json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[GameEventManager] No GameEvents.json found at {path}");
            return;
        }

        _config = JsonUtility.FromJson<GameEventsConfig>(File.ReadAllText(path));
    }

    private void OnEventCompleted(string key)
    {
        foreach (var binding in bindings)
        {
            if (binding.eventKey == key)
                binding.onTriggered.Invoke();
        }

        if (_config?.spawnBindings == null)
            return;

        foreach (var data in _config.spawnBindings)
        {
            if (data.eventKey != key)
                continue;

            GameObject prefab = Resources.Load<GameObject>($"Spawnable/{data.prefabName}");
            if (prefab == null)
                continue;

            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            if (!string.IsNullOrEmpty(data.spawnPointName))
            {
                GameObject spawnPoint = GameObject.Find(data.spawnPointName);
                if (spawnPoint != null)
                {
                    pos = spawnPoint.transform.position;
                    rot = spawnPoint.transform.rotation;
                }
            }

            NetworkServer.Spawn(Instantiate(prefab, pos, rot));
        }
    }
}
