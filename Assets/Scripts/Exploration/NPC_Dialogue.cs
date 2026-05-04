// NPC_Dialogue.cs
using UnityEngine;
using System.Collections;

public class NPC_Dialogue : InteractableBase
{
    [Header("NPC 정보")]
    public string npcName;

    [Header("대사 (대화마다 배열 하나)")]
    [TextArea(2, 6)]
    public string[] dialogueLines;

    [Header("한 번만 말하는 추가 대사")]
    [TextArea(2, 6)]
    public string[] firstTimeOnly;
    private bool hasSpoken = false;

    public override void Interact(PlayerTopDown player)
    {
        player.isLocked = true;

        string[] lines = (!hasSpoken && firstTimeOnly.Length > 0)
            ? firstTimeOnly
            : dialogueLines;

        hasSpoken = true;

        StartCoroutine(DialogueManager.Instance.PlayDialogue(
            npcName, lines,
            onFinish: () => player.isLocked = false
        ));
    }
}