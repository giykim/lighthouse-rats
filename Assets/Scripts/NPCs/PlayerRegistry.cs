using System.Collections.Generic;
using UnityEngine;

// Add this component to your Player prefab.
// EnemyAI reads from PlayerRegistry.All instead of FindGameObjectsWithTag every frame.
public class PlayerRegistry : MonoBehaviour
{
    public static readonly List<Transform> All = new();

    private void OnEnable()  => All.Add(transform);
    private void OnDisable() => All.Remove(transform);
}
