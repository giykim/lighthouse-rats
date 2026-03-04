using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialoguePhase
{
    public string[] lines;
    public string triggerEvent;
}

[Serializable]
public class DialogueEntry
{
    public string[] requiredEvents;
    public DialoguePhase[] phases;
}

[Serializable]
public class NPCDialogueData
{
    public List<DialogueEntry> entries = new();

    public int GetActiveEntryIndex()
    {
        if (GameProgress.Instance == null)
        {
            return -1;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            bool conditionsMet = true;
            foreach (var required in entries[i].requiredEvents ?? Array.Empty<string>())
            {
                if (!GameProgress.Instance.Has(required))
                {
                    conditionsMet = false;
                    break;
                }
            }
            if (conditionsMet)
            {
                return i;
            }
        }
        return -1;
    }
}
