using System.Collections.Generic;
using UnityEngine;

public class PlayerRegistry : MonoBehaviour
{
    public static readonly List<Transform> All = new();

    private void OnEnable()  => All.Add(transform);
    private void OnDisable() => All.Remove(transform);
}
