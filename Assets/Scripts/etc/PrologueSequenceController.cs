using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class PrologueDialogueLine
{
    public string speakerName;

    [TextArea(2, 6)]
    public string dialogue;

    public Sprite characterSprite;
    public AudioClip voiceClip;
}

public class PrologueSequenceController : MonoBehaviour
{
    [Header("Canvas Flow")]
    [SerializeField] private CanvasGroup canvas1Group;
    [SerializeField] private CanvasGroup canvas2Group;
    [SerializeField] private GameObject dialogueCanvasRoot;
    [SerializeField] private CanvasGroup dialogueGroup;
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private RectTransform titleImage;
    [SerializeField] private RectTransform topEyeCover;
    [SerializeField] private RectTransform bottomEyeCover;

    [Header("Dialogue UI")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Image characterImage;
    [SerializeField] private Image canvas2MouthImage;
    [SerializeField] private Sprite mouthOpenSprite;
    [SerializeField] private Sprite mouthClosedSprite;

    [Header("Dialogue")]
    public PrologueDialogueLine[] canvas1Lines;
    public PrologueDialogueLine[] canvas2Lines;

    [Header("Timing")]
    [SerializeField] private float typingSpeed = 0.035f;
    [SerializeField] private float lineEndDelay = 0.08f;
    [SerializeField] private float fadeDuration = 0.75f;
    [SerializeField] private float titleFadeDuration = 1f;
    [SerializeField] private float titleHoldSeconds = 2f;
    [SerializeField] private float titleAppearDelayAfterDialogue = 1.5f;
    [SerializeField] private float titleFadeAfterCanvas2Delay = 1f;
    [SerializeField] private float eyeOpenDuration = 1.15f;
    [SerializeField] private float mouthCycleDuration = 0.24f;

    [Header("Audio")]
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioSource titleMusicSource;
    [SerializeField] private AudioClip titleMusicClip;
    [SerializeField] private float titleMusicVolume = 1f;
    [SerializeField] private float titleMusicFadeInDuration = 1.5f;

    private int lineIndex;
    private bool isTyping;
    private bool advanceRequested;
    private bool skipTypingRequested;
    private Coroutine typingRoutine;
    private Coroutine mouthRoutine;
    private Coroutine titleMusicFadeRoutine;
    private Vector2 topEyeClosedPosition;
    private Vector2 bottomEyeClosedPosition;
    private PrologueDialogueLine[] currentLines;
    private bool titleTransitionPlayed;

    private void Awake()
    {
        EnsureDialogueCanvas();
        PrepareCanvas2();

        SetGroup(canvas1Group, 1f, true);
        SetGroup(canvas2Group, 0f, false);
        SetDialogueVisible(true);
        SetGroup(titleGroup, 0f, false);
        CacheEyeCoverPositions();
        SetEyeCoversVisible(false);
        SetMouthClosed();
        SetMouthVisible(false);

        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(false);
            BringTitleToFront();
        }
    }

    private void Start()
    {
        StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        yield return PlayCanvas1();
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

    private IEnumerator PlayCanvas1()
    {
        if (canvas1Lines == null || canvas1Lines.Length == 0)
        {
            yield return PlayTitleTransition();
            yield break;
        }

        yield return PlayDialogueLines(canvas1Lines, true);
        yield return FadeGroup(dialogueGroup, 1f, 0f, fadeDuration, false);
        yield return new WaitForSecondsRealtime(titleAppearDelayAfterDialogue);
        yield return PlayTitleTransition();
    }

    private IEnumerator PlayDialogueLines(PrologueDialogueLine[] lines, bool transitionWhenEmpty)
    {
        if (lines == null || lines.Length == 0)
        {
            if (transitionWhenEmpty)
            {
                yield return PlayTitleTransition();
            }

            yield break;
        }

        currentLines = lines;
        SetMouthVisible(lines == canvas2Lines);

        for (lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            ShowLine(lines[lineIndex]);
            typingRoutine = StartCoroutine(TypeLine(lines[lineIndex].dialogue));
            yield return typingRoutine;

            yield return new WaitForSecondsRealtime(lineEndDelay);
            advanceRequested = false;
            yield return new WaitUntil(() => advanceRequested);
            advanceRequested = false;
        }

        SetMouthVisible(false);
    }

    private void EnsureDialogueCanvas()
    {
        if (dialogueCanvasRoot == null && dialogueGroup != null)
        {
            dialogueCanvasRoot = dialogueGroup.gameObject;
        }

        if (dialogueCanvasRoot == null)
        {
            return;
        }

        dialogueCanvasRoot.SetActive(true);

        if (dialogueCanvasRoot.transform.localScale == Vector3.zero)
        {
            dialogueCanvasRoot.transform.localScale = Vector3.one;
        }

        Canvas dialogueCanvas = dialogueCanvasRoot.GetComponent<Canvas>();

        if (dialogueCanvas != null)
        {
            dialogueCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            dialogueCanvas.worldCamera = Camera.main;
            dialogueCanvas.planeDistance = 1f;
            dialogueCanvas.overrideSorting = true;
            dialogueCanvas.sortingOrder = 100;
        }

        if (dialogueGroup == null)
        {
            dialogueGroup = dialogueCanvasRoot.GetComponent<CanvasGroup>();

            if (dialogueGroup == null)
            {
                dialogueGroup = dialogueCanvasRoot.AddComponent<CanvasGroup>();
            }
        }
    }

    private void SetDialogueVisible(bool visible)
    {
        EnsureDialogueCanvas();

        if (dialogueGroup != null)
        {
            SetGroup(dialogueGroup, visible ? 1f : 0f, visible);
        }
        else if (dialogueCanvasRoot != null)
        {
            dialogueCanvasRoot.SetActive(visible);
        }
    }

    private void ShowLine(PrologueDialogueLine line)
    {
        if (nameText != null)
        {
            nameText.text = line.speakerName;
        }

        if (characterImage != null)
        {
            characterImage.sprite = line.characterSprite;
            characterImage.gameObject.SetActive(line.characterSprite != null);
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
        dialogueText.text = "";

        if (line == null)
        {
            line = "";
        }

        if (ShouldAnimateMouth())
        {
            if (mouthRoutine != null)
            {
                StopCoroutine(mouthRoutine);
            }

            mouthRoutine = StartCoroutine(AnimateMouthForLine(line));
        }

        foreach (char character in line)
        {
            if (skipTypingRequested)
            {
                break;
            }
            dialogueText.text += character;
            yield return new WaitForSecondsRealtime(typingSpeed);
        }

        dialogueText.text = line;
        isTyping = false;
        skipTypingRequested = false;
    }

    private bool ShouldAnimateMouth()
    {
        return currentLines == canvas2Lines
            && canvas2MouthImage != null
            && mouthOpenSprite != null
            && mouthClosedSprite != null;
    }

    private IEnumerator AnimateMouthForLine(string line)
    {
        int cycleCount = CountVisibleCharacters(line);
        float halfCycleDuration = Mathf.Max(0.02f, mouthCycleDuration * 0.5f);

        for (int i = 0; i < cycleCount; i++)
        {
            if (skipTypingRequested)
            {
                break;
            }

            SetMouthOpen();
            yield return new WaitForSecondsRealtime(halfCycleDuration);

            SetMouthClosed();
            yield return new WaitForSecondsRealtime(halfCycleDuration);
        }

        SetMouthClosed();
        mouthRoutine = null;
    }

    private int CountVisibleCharacters(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            return 0;
        }

        int count = 0;

        foreach (char character in line)
        {
            if (!char.IsWhiteSpace(character))
            {
                count++;
            }
        }

        return count;
    }

    private void SetMouthVisible(bool visible)
    {
        if (canvas2MouthImage != null)
        {
            canvas2MouthImage.gameObject.SetActive(visible);
        }

        if (!visible && mouthRoutine != null)
        {
            StopCoroutine(mouthRoutine);
            mouthRoutine = null;
            SetMouthClosed();
        }
    }

    private void SetMouthOpen()
    {
        if (canvas2MouthImage != null && mouthOpenSprite != null)
        {
            canvas2MouthImage.sprite = mouthOpenSprite;
        }
    }

    private void SetMouthClosed()
    {
        if (canvas2MouthImage != null)
        {
            if (mouthClosedSprite != null)
            {
                canvas2MouthImage.sprite = mouthClosedSprite;
            }
        }
    }

    private IEnumerator PlayTitleTransition()
    {
        if (titleTransitionPlayed)
        {
            yield break;
        }

        titleTransitionPlayed = true;

        if (voiceSource != null)
        {
            voiceSource.Stop();
        }

        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(true);
            BringTitleToFront();
        }

        StartTitleMusic();
        yield return FadeGroup(titleGroup, 0f, 1f, titleFadeDuration, true);
        yield return new WaitForSecondsRealtime(titleHoldSeconds);
        PrepareCanvas2();
        SetGroup(canvas2Group, 1f, true);
        yield return OpenEyeCovers();
        BringTitleToFront();
        yield return new WaitForSecondsRealtime(titleFadeAfterCanvas2Delay);
        SetGroup(titleGroup, 0f, false);

        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(false);
        }

        SetDialogueVisible(true);
        yield return PlayDialogueLines(canvas2Lines, false);
    }

    private void StartTitleMusic()
    {
        if (titleMusicSource == null)
        {
            return;
        }

        if (titleMusicClip != null)
        {
            titleMusicSource.clip = titleMusicClip;
        }

        if (titleMusicSource.clip == null)
        {
            return;
        }

        if (titleMusicFadeRoutine != null)
        {
            StopCoroutine(titleMusicFadeRoutine);
        }

        titleMusicSource.volume = 0f;

        if (!titleMusicSource.isPlaying)
        {
            titleMusicSource.Play();
        }

        titleMusicFadeRoutine = StartCoroutine(FadeAudio(titleMusicSource, titleMusicVolume, titleMusicFadeInDuration));
    }

    private IEnumerator FadeAudio(AudioSource source, float targetVolume, float duration)
    {
        if (source == null)
        {
            yield break;
        }

        float startVolume = source.volume;
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            source.volume = Mathf.Lerp(startVolume, targetVolume, t);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        source.volume = targetVolume;
        titleMusicFadeRoutine = null;
    }

    private string GetCurrentLineText()
    {
        if (currentLines != null && lineIndex >= 0 && lineIndex < currentLines.Length)
        {
            return currentLines[lineIndex].dialogue;
        }

        return "";
    }

    private void PrepareCanvas2()
    {
        if (canvas2Group == null)
        {
            return;
        }

        Transform canvas2Transform = canvas2Group.transform;

        if (canvas2Transform.localScale == Vector3.zero)
        {
            canvas2Transform.localScale = Vector3.one;
        }

        Canvas canvas2 = canvas2Group.GetComponent<Canvas>();

        if (canvas2 != null)
        {
            canvas2.renderMode = RenderMode.ScreenSpaceCamera;
            canvas2.worldCamera = Camera.main;
            canvas2.overrideSorting = true;
            canvas2.sortingOrder = 5;
        }

        Transform background = canvas2Transform.Find("HouseMasterBackground");

        if (background != null)
        {
            background.gameObject.SetActive(true);
            background.SetAsFirstSibling();
        }
    }

    private void CacheEyeCoverPositions()
    {
        if (topEyeCover != null)
        {
            topEyeClosedPosition = topEyeCover.anchoredPosition;
        }

        if (bottomEyeCover != null)
        {
            bottomEyeClosedPosition = bottomEyeCover.anchoredPosition;
        }
    }

    private void SetEyeCoversVisible(bool visible)
    {
        if (topEyeCover != null)
        {
            topEyeCover.gameObject.SetActive(visible);
            topEyeCover.anchoredPosition = topEyeClosedPosition;
        }

        if (bottomEyeCover != null)
        {
            bottomEyeCover.gameObject.SetActive(visible);
            bottomEyeCover.anchoredPosition = bottomEyeClosedPosition;
        }

        BringTitleToFront();
    }

    private void BringTitleToFront()
    {
        if (titleImage != null)
        {
            titleImage.SetAsLastSibling();
        }
    }

    private IEnumerator OpenEyeCovers()
    {
        if (topEyeCover == null || bottomEyeCover == null)
        {
            yield break;
        }

        SetEyeCoversVisible(true);

        float topDistance = topEyeCover.rect.height + 80f;
        float bottomDistance = bottomEyeCover.rect.height + 80f;
        Vector2 topOpenPosition = topEyeClosedPosition + Vector2.up * topDistance;
        Vector2 bottomOpenPosition = bottomEyeClosedPosition + Vector2.down * bottomDistance;
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = eyeOpenDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / eyeOpenDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            topEyeCover.anchoredPosition = Vector2.Lerp(topEyeClosedPosition, topOpenPosition, eased);
            bottomEyeCover.anchoredPosition = Vector2.Lerp(bottomEyeClosedPosition, bottomOpenPosition, eased);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        SetEyeCoversVisible(false);
    }

    private IEnumerator FadeGroup(CanvasGroup group, float from, float to, float duration, bool activeAfter)
    {
        if (group == null)
        {
            yield break;
        }

        group.gameObject.SetActive(true);
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            group.alpha = Mathf.Lerp(from, to, t);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        group.alpha = to;
        SetGroup(group, to, activeAfter);
    }

    private void SetGroup(CanvasGroup group, float alpha, bool active)
    {
        if (group == null)
        {
            return;
        }

        group.alpha = alpha;
        group.interactable = active;
        group.blocksRaycasts = active;

        if (active && group.transform.localScale == Vector3.zero)
        {
            group.transform.localScale = Vector3.one;
        }

        group.gameObject.SetActive(active || alpha > 0f);
    }
}
