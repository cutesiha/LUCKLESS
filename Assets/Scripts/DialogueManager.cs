using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject dialoguePanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;

    [Header("Dialogue Data")]
    public string[] speakerNames;
    [TextArea(2, 5)]
    public string[] dialogueLines;

    private int currentIndex = 0;
    private bool isDialogueActive = false;

    void Start()
    {
        StartDialogue();
    }

    void Update()
    {
        if (!isDialogueActive) return;

        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            NextLine();
        }
    }

    public void StartDialogue()
    {
        if (speakerNames.Length == 0 || dialogueLines.Length == 0)
        {
            Debug.LogWarning("대사 데이터가 비어 있습니다.");
            return;
        }

        if (speakerNames.Length != dialogueLines.Length)
        {
            Debug.LogWarning("이름 배열과 대사 배열 길이가 다릅니다.");
            return;
        }

        currentIndex = 0;
        isDialogueActive = true;
        dialoguePanel.SetActive(true);
        ShowLine();
    }

    void ShowLine()
    {
        nameText.text = speakerNames[currentIndex];
        dialogueText.text = dialogueLines[currentIndex];
    }

    public void NextLine()
    {
        currentIndex++;

        if (currentIndex >= dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        ShowLine();
    }

    void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        Debug.Log("대화 종료");
    }
}