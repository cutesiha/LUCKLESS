using System.Collections;
using UnityEngine;
using TMPro;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(2, 5)]
    public string line;
}

[System.Serializable]
public class CanvasDialogue
{
    public GameObject canvasObject;
    public DialogueLine[] lines;
}

public class TypewriterCanvasDialogue : MonoBehaviour
{
    [Header("Dialogue Order")]
    public CanvasDialogue[] canvasDialogues;

    [Header("Text")]
    public TMP_Text dialogueText;
    public TMP_Text nameText;

    [Header("Typing")]
    public float typingSpeed = 0.04f;
    public float lineDelay = 0.2f; // 줄바꿈 딜레이

    private int canvasIndex = 0;
    private int lineIndex = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Start()
    {
        ShowCanvas(canvasIndex);
        StartLine();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            Next();
        }
    }

    void Next()
    {
        if (isTyping)
        {
            StopCoroutine(typingCoroutine);
            dialogueText.text = canvasDialogues[canvasIndex].lines[lineIndex].line;
            isTyping = false;
            return;
        }

        lineIndex++;

        if (lineIndex < canvasDialogues[canvasIndex].lines.Length)
        {
            StartLine();
        }
        else
        {
            canvasIndex++;
            lineIndex = 0;

            if (canvasIndex < canvasDialogues.Length)
            {
                ShowCanvas(canvasIndex);
                StartLine();
            }
            else
            {
                dialogueText.text = "";
                nameText.text = "";
                Debug.Log("모든 대화 종료");
            }
        }
    }

    void ShowCanvas(int index)
    {
        for (int i = 0; i < canvasDialogues.Length; i++)
        {
            if (canvasDialogues[i].canvasObject != null)
                canvasDialogues[i].canvasObject.SetActive(i == index);
        }
    }

    void StartLine()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        nameText.text = canvasDialogues[canvasIndex].lines[lineIndex].speakerName;

        typingCoroutine = StartCoroutine(
            TypeLine(canvasDialogues[canvasIndex].lines[lineIndex].line)
        );
    }

    IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char c in line)
        {
            dialogueText.text += c;

            if (c == '\n')
                yield return new WaitForSeconds(lineDelay);
            else
                yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }
}