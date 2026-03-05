using Mirror;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;

public class TalkableNPC : InteractableObject
{
    [SerializeField]
    private string dialogueFile;

    private NPCDialogueData _dialogue;
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

    public override void OnStartServer()
    {
        if (string.IsNullOrEmpty(dialogueFile))
            return;

        string path = Path.Combine(Application.streamingAssetsPath, "Dialogue", dialogueFile + ".json");
        if (!File.Exists(path))
        {
            Debug.LogWarning($"[TalkableNPC] Dialogue file not found: {path}");
            return;
        }

        _dialogue = JsonUtility.FromJson<NPCDialogueData>(File.ReadAllText(path));
    }

    public override string GetPromptText(PlayerController player) => "Press E to talk";

    public override void OnInteract(PlayerController player)
    {
        CommandStartDialogue(player.netIdentity);
    }

    [Command(requiresAuthority = false)]
    private void CommandStartDialogue(NetworkIdentity playerIdentity)
    {
        if (_dialogueActive || _dialogue == null)
        {
            return;
        }

        _talkingTo = playerIdentity.transform;

        int entryIndex = _dialogue.GetActiveEntryIndex();
        if (entryIndex == -1)
        {
            return;
        }

        if (entryIndex != _activeEntryIndex)
        {
            _activeEntryIndex = entryIndex;
            _phaseIndex = 0;
        }

        DialogueEntry entry = _dialogue.entries[entryIndex];
        if (entry.phases == null || entry.phases.Length == 0)
        {
            return;
        }

        DialoguePhase phase = entry.phases[_phaseIndex];
        _phaseIndex = Mathf.Min(_phaseIndex + 1, entry.phases.Length - 1);

        if (!string.IsNullOrEmpty(phase.triggerEvent))
            GameProgress.Instance.Complete(phase.triggerEvent);

        string[] lines = phase.lines;

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
            yield return new WaitForSeconds(secondsPerLine / (DebugService.FastDialogue ? DebugService.FastDialogueMultiplier : 1f));
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
