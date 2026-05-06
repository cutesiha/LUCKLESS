using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractionObject : MonoBehaviour
{
    [Header("NPC 설정")]
    public Sprite npcSprite;
    public string npcName = "NPC";

    [TextArea(2, 5)]
    public string dialogueText;

    [Header("대화 UI")]
    public GameObject dialoguePanel;
    public Image npcImageUI;
    public TMP_Text nameText;
    public TMP_Text dialogueTextUI;

    private bool playerInside = false;

    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    void Update()
    {
        if (playerInside)
        {
            ShowDialogue();
        }
    }

    private void ShowDialogue()
    {
        dialoguePanel.SetActive(true);

        if (npcImageUI != null)
        {
            npcImageUI.sprite = npcSprite;
            npcImageUI.gameObject.SetActive(npcSprite != null);
        }

        if (nameText != null)
            nameText.text = npcName;

        if (dialogueTextUI != null)
            dialogueTextUI.text = dialogueText;
    }

    private void HideDialogue()
    {
        dialoguePanel.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("닿음: " + other.name);

        if (!other.CompareTag("Player")) return;

        ShowDialogue();
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            HideDialogue();
        }
    }
}