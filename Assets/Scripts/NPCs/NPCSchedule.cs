using System;
using UnityEngine;

public enum NPCBehavior { Idle, Patrol, Sleep, Investigate }

[Serializable]
public class ScheduleEntry
{
    public float startHour;
    public float endHour;
    public NPCBehavior behavior;
    public Transform[] waypoints;
}
