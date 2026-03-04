using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialoguePhase
{
    [TextArea]
    public string[] lines;
}

[Serializable]
public class DialogueEntry
{
    public string[] requiredEvents;
    public DialoguePhase[] phases;
}

[CreateAssetMenu(fileName = "NPCDialogue", menuName = "NPCs/Dialogue")]
public class NPCDialogue : ScriptableObject
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
            foreach (var required in entries[i].requiredEvents)
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
