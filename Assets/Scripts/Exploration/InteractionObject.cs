using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class InteractionObject : MonoBehaviour
{
    [Header("NPC 설정")]
    public Sprite npcSprite;
    public string npcName = "NPC";

    [TextArea(2, 5)]
    public string dialogueText;

    [Header("대화 UI")]
    public Canvas dialogueCanvas;
    public GameObject dialoguePanel;
    public Image npcImageUI;
    public TMP_Text nameText;
    public TMP_Text dialogueTextUI;

    private bool dialogueRunning = false;
    private PlayerTopDown currentPlayer;

    void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
        if (npcImageUI != null)
            npcImageUI.gameObject.SetActive(false);
    }

    private void ShowDialogue()
    {
        if (dialoguePanel == null)
        {
            Debug.LogWarning("[InteractionObject] dialoguePanel 미연결", this);
            return;
        }

        // 패널 부모 Canvas가 꺼져 있으면 패널만 켜도 화면에 보이지 않음
        if (dialogueCanvas == null)
            dialogueCanvas = dialoguePanel.GetComponentInParent<Canvas>(true);
        if (dialogueCanvas != null && !dialogueCanvas.gameObject.activeSelf)
            dialogueCanvas.gameObject.SetActive(true);

        dialoguePanel.SetActive(true);
        Debug.Log($"[InteractionObject] ShowDialogue panel:{dialoguePanel.activeSelf} inHierarchy:{dialoguePanel.activeInHierarchy}", this);

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
        if (dialoguePanel == null) return;
        dialoguePanel.SetActive(false);
        if (npcImageUI != null)
            npcImageUI.gameObject.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("닿음: " + other.name);

        if (!other.CompareTag("Player")) return;
        if (dialogueRunning) return;

        StartCoroutine(BeginDialogueFlow(other));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        StopAllCoroutines();
        if (currentPlayer != null) currentPlayer.isLocked = false;
        currentPlayer = null;
        HideDialogue();
        dialogueRunning = false;
    }

    private IEnumerator BeginDialogueFlow(Collider2D playerCollider)
    {
        dialogueRunning = true;

        // 플레이어 이동 잠금 (해당 컴포넌트가 있을 때만)
        currentPlayer = playerCollider.GetComponent<PlayerTopDown>();
        if (currentPlayer != null) currentPlayer.isLocked = true;

        ShowDialogue();
        yield return new WaitUntil(() =>
            Input.GetKeyDown(KeyCode.Z) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetMouseButtonDown(0)
        );
        HideDialogue();

        if (currentPlayer != null) currentPlayer.isLocked = false;
        currentPlayer = null;
        dialogueRunning = false;
    }
}