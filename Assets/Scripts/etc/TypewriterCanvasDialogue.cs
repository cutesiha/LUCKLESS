using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(2, 5)]
    public string line;

    public Sprite characterSprite;
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

    [Header("Common Dialogue UI")]
    public GameObject dialogueCanvas;

    [Header("Text")]
    public TMP_Text dialogueText;
    public TMP_Text nameText;
    public Image characterImage;

    [Header("Typing")]
    public float typingSpeed = 0.04f;
    public float lineDelay = 0.2f;

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
                    StartCoroutine(ChangeCanvasWithFade());
                }
            else
            {
                dialogueText.text = "";
                nameText.text = "";
                characterImage.gameObject.SetActive(false);
                Debug.Log("모든 대화 종료");
            }
        }
    }

    public Animator fadeAnimator;
    public float fadeTime = 0.5f;

    IEnumerator ChangeCanvasWithFade()
    {
        fadeAnimator.SetTrigger("FadeOut");

        yield return new WaitForSeconds(fadeTime);

        ShowCanvas(canvasIndex);
        StartLine();

        fadeAnimator.SetTrigger("FadeIn");
    }
    
    void ShowCanvas(int index)
    {
        if (dialogueCanvas != null)
        {
            dialogueCanvas.SetActive(true);
        }

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

        var line = canvasDialogues[canvasIndex].lines[lineIndex];

        nameText.text = line.speakerName;

        if (line.characterSprite != null)
        {
            characterImage.sprite = line.characterSprite;
            characterImage.gameObject.SetActive(true);
        }
        else
        {
            characterImage.gameObject.SetActive(false);
        }

        typingCoroutine = StartCoroutine(TypeLine(line.line));
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