using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;

public class TalkableNPC : InteractableObject
{
    [SerializeField]
    private NPCDialogue dialogue;
    [SerializeField]
    private TextMeshPro dialogueText;
    [SerializeField]
    private GameObject dialogueBubble;
    [SerializeField]
    private float secondsPerLine = 3f;

    private Coroutine _dialogueCoroutine;
    
    private int _activeEntryIndex = -1;
    private int _phaseIndex;

    private void Awake()
    {
        dialogueBubble.SetActive(false);
    }

    public override string GetPromptText() => "Press E to talk";

    public override void OnInteract(PlayerController player)
    {
        CommandStartDialogue();
    }

    [Command(requiresAuthority = false)]
    private void CommandStartDialogue()
    {
        if (dialogue == null)
        {
            return;
        }

        int entryIndex = dialogue.GetActiveEntryIndex();
        if (entryIndex == -1)
        {
            return;
        }

        if (entryIndex != _activeEntryIndex)
        {
            _activeEntryIndex = entryIndex;
            _phaseIndex = 0;
        }

        DialogueEntry entry = dialogue.entries[entryIndex];
        if (entry.phases == null || entry.phases.Length == 0) return;

        string[] lines = entry.phases[_phaseIndex].lines;
        
        _phaseIndex = Mathf.Min(_phaseIndex + 1, entry.phases.Length - 1);

        RpcShowDialogue(lines);
    }

    [ClientRpc]
    private void RpcShowDialogue(string[] lines)
    {
        if (_dialogueCoroutine != null)
        {
            StopCoroutine(_dialogueCoroutine);
        }

        _dialogueCoroutine = StartCoroutine(ShowLines(lines));
    }

    private IEnumerator ShowLines(string[] lines)
    {
        dialogueBubble.SetActive(true);
        foreach (var line in lines)
        {
            dialogueText.text = line;
            yield return new WaitForSeconds(secondsPerLine);
        }
        dialogueBubble.SetActive(false);
        dialogueText.text = string.Empty;
        _dialogueCoroutine = null;
    }

    private void LateUpdate()
    {
        if (!dialogueBubble.activeSelf)
        {
            return;
        }

        if (Camera.main != null)
        {
            dialogueBubble.transform.forward = Camera.main.transform.forward;
        }
    }
}
