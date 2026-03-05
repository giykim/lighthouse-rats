using System;

[Serializable]
public class SaveData
{
    public int currentDay = 1;
    public string[] completedEvents = Array.Empty<string>();
}
