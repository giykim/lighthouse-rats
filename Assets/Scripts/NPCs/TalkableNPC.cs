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
    private bool _dialogueActive;
    private Transform _talkingTo;

    private int _activeEntryIndex = -1;
    private int _phaseIndex;

    private void Awake()
    {
        dialogueBubble.SetActive(false);
    }

    public override string GetPromptText() => "Press E to talk";

    public override void OnInteract(PlayerController player)
    {
        CommandStartDialogue(player.netIdentity);
    }

    [Command(requiresAuthority = false)]
    private void CommandStartDialogue(NetworkIdentity playerIdentity)
    {
        if (_dialogueActive || dialogue == null)
        {
            return;
        }

        _talkingTo = playerIdentity.transform;

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
        if (entry.phases == null || entry.phases.Length == 0)
        {
            return;
        }

        string[] lines = entry.phases[_phaseIndex].lines;
        _phaseIndex = Mathf.Min(_phaseIndex + 1, entry.phases.Length - 1);

        _dialogueActive = true;
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
        _dialogueActive = false;
        _talkingTo = null;
    }

    private void Update()
    {
        if (!isServer || !_dialogueActive || _talkingTo == null)
        {
            return;
        }

        Vector3 dir = _talkingTo.position - transform.position;
        dir.y = 0f;
        if (dir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void LateUpdate()
    {
        if (!dialogueBubble.activeSelf)
        {
            return;
        }

        if (Camera.main != null)
        {
            dialogueBubble.transform.forward = dialogueBubble.transform.position - Camera.main.transform.position;
        }
    }
}
