using System.Collections.Generic;
using UnityEngine;

public class PlayerRegistry : MonoBehaviour
{
    public static readonly List<Transform> All = new();

    public static List<Transform> AllAlive()
    {
        List<Transform> alive = new();
        foreach (var t in All)
        {
            if (t == null)
            {
                continue;
            }

            PlayerHealth health = t.GetComponent<PlayerHealth>();
            if (health == null || health.IsAlive)
            {
                alive.Add(t);
            }
        }
        return alive;
    }

    private void OnEnable()  => All.Add(transform);
    private void OnDisable() => All.Remove(transform);
}
