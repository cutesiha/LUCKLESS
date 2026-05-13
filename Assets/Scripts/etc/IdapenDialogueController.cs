using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class IdapenDialogueLine
{
    public string speakerName;

    [TextArea(2, 6)]
    public string dialogue;

    public Sprite characterSprite;
    public AudioClip voiceClip;
}

public class IdapenDialogueController : MonoBehaviour
{
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialogueCanvasPrefab;
    [SerializeField] private GameObject dialogueCanvasRoot;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image characterImage;
    [SerializeField] private AudioSource voiceSource;

    [Header("Dialogue")]
    [SerializeField] private IdapenDialogueLine[] lines = new IdapenDialogueLine[1];

    [Header("Timing")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float lineEndDelay = 0.08f;

    private int lineIndex;
    private bool isTyping;
    private bool advanceRequested;
    private bool skipTypingRequested;
    private Coroutine typingRoutine;

    private void Awake()
    {
        EnsureDialogueCanvas();
        SetDialogueVisible(true);
    }

    private void Start()
    {
        StartCoroutine(PlayDialogue());
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
        {
            if (isTyping)
            {
                skipTypingRequested = true;
                return;
            }

            advanceRequested = true;
        }
    }

    private IEnumerator PlayDialogue()
    {
        if (lines == null || lines.Length == 0)
        {
            yield break;
        }

        for (lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            IdapenDialogueLine line = lines[lineIndex];
            if (line == null)
            {
                continue;
            }

            ShowLine(line);
            typingRoutine = StartCoroutine(TypeLine(line.dialogue));
            yield return typingRoutine;
            typingRoutine = null;

            yield return new WaitForSecondsRealtime(lineEndDelay);
            advanceRequested = false;
            yield return new WaitUntil(() => advanceRequested);
        }
    }

    private void ShowLine(IdapenDialogueLine line)
    {
        if (nameText != null)
        {
            nameText.text = line.speakerName;
        }

        if (characterImage != null)
        {
            characterImage.sprite = line.characterSprite;
            characterImage.enabled = line.characterSprite != null;
            characterImage.preserveAspect = true;
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
            voiceSource.clip = line.voiceClip;
            if (line.voiceClip != null)
            {
                voiceSource.Play();
            }
        }
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        skipTypingRequested = false;

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        string text = line ?? "";
        StringBuilder builder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (skipTypingRequested)
            {
                break;
            }

            builder.Append(text[i]);
            if (dialogueText != null)
            {
                dialogueText.text = builder.ToString();
            }

            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        if (dialogueText != null)
        {
            dialogueText.text = text;
        }

        isTyping = false;
    }

    private void EnsureDialogueCanvas()
    {
        if (dialogueCanvasRoot == null && dialogueCanvasPrefab != null)
        {
            dialogueCanvasRoot = Instantiate(dialogueCanvasPrefab);
            dialogueCanvasRoot.name = dialogueCanvasPrefab.name;
        }

        if (dialogueCanvasRoot == null)
        {
            return;
        }

        Canvas canvas = dialogueCanvasRoot.GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
        }

        if (nameText == null)
        {
            nameText = FindChildComponent<TMP_Text>(dialogueCanvasRoot.transform, "NameText");
        }

        if (dialogueText == null)
        {
            dialogueText = FindChildComponent<TMP_Text>(dialogueCanvasRoot.transform, "DialogueText");
        }

        if (characterImage == null)
        {
            characterImage = FindChildComponent<Image>(dialogueCanvasRoot.transform, "CharacterImage");
        }
    }

    private T FindChildComponent<T>(Transform root, string childName) where T : Component
    {
        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            if (children[i].name == childName)
            {
                return children[i].GetComponent<T>();
            }
        }

        return null;
    }

    private void SetDialogueVisible(bool visible)
    {
        if (dialogueCanvasRoot != null)
        {
            dialogueCanvasRoot.SetActive(visible);
        }
    }
}
