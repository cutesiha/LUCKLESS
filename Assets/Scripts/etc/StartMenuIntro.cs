using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartMenuIntro : MonoBehaviour
{
    [Header("Intro")]
    [SerializeField] private RectTransform title;
    [SerializeField] private CanvasGroup clickPrompt;
    [SerializeField] private float titleRiseDuration = 1.65f;
    [SerializeField] private float titleStartOffset = 1350f;
    [SerializeField] private float clickBlinkSpeed = 1.55f;
    [SerializeField] private Color clickPromptOutlineColor = new Color(0.16f, 0.16f, 0.16f, 0.50f);
    [SerializeField] private float clickPromptOutlineDistance = 1.7f;

    [Header("Buttons")]
    [SerializeField] private RectTransform[] menuButtons;
    [SerializeField] private float buttonSlideDuration = 0.82f;
    [SerializeField] private float buttonStagger = 0.18f;
    [SerializeField] private float buttonStartOffset = 2200f;

    private Vector2 titleTarget;
    private Vector2[] buttonTargets;
    private bool canOpenButtons;
    private bool buttonsOpening;
    private Coroutine introRoutine;
    private TextMeshProUGUI clickPromptText;
    private TextMeshProUGUI clickPromptForegroundText;
    private Color clickPromptTextColor = Color.white;
    private readonly TextMeshProUGUI[] clickPromptOutlineTexts = new TextMeshProUGUI[ClickPromptOutlineOffsets.Length];

    private static readonly Vector2[] ClickPromptOutlineOffsets =
    {
        new Vector2(-1f, 0f),
        new Vector2(1f, 0f),
        new Vector2(0f, -1f),
        new Vector2(0f, 1f),
        new Vector2(-1f, -1f),
        new Vector2(-1f, 1f),
        new Vector2(1f, -1f),
        new Vector2(1f, 1f)
    };

    private void Awake()
    {
        if (title != null)
        {
            titleTarget = title.anchoredPosition;
        }

        if (clickPrompt != null)
        {
            clickPrompt.interactable = false;
            clickPrompt.blocksRaycasts = false;
            clickPromptText = clickPrompt.GetComponentInChildren<TextMeshProUGUI>(true);
            if (clickPromptText != null)
            {
                clickPromptTextColor = clickPromptText.color;
            }
            RefreshClickPromptOutline();
        }

        buttonTargets = new Vector2[menuButtons.Length];

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            buttonTargets[i] = menuButtons[i].anchoredPosition;
        }
    }

    private void OnEnable()
    {
        if (introRoutine != null)
        {
            StopCoroutine(introRoutine);
        }

        ResetIntroState();
        introRoutine = StartCoroutine(PlayIntro());
    }

    private void ResetIntroState()
    {
        canOpenButtons = false;
        buttonsOpening = false;

        if (title != null)
        {
            title.anchoredPosition = titleTarget + Vector2.down * titleStartOffset;
            title.gameObject.SetActive(true);
        }

        if (clickPrompt != null)
        {
            clickPrompt.alpha = 0f;
            clickPrompt.gameObject.SetActive(false);
            RefreshClickPromptOutline();
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            menuButtons[i].anchoredPosition = buttonTargets[i] + Vector2.right * buttonStartOffset;
            menuButtons[i].gameObject.SetActive(false);
        }
    }

    private IEnumerator PlayIntro()
    {
        if (title != null)
        {
            yield return Move(title, title.anchoredPosition, titleTarget, titleRiseDuration);
        }

        if (clickPrompt != null)
        {
            clickPrompt.gameObject.SetActive(true);
            RefreshClickPromptOutline();
        }

        canOpenButtons = true;
    }

    private void Update()
    {
        if (clickPrompt != null && clickPrompt.gameObject.activeSelf && canOpenButtons && !buttonsOpening)
        {
            clickPrompt.alpha = 0.35f + Mathf.PingPong(Time.unscaledTime * clickBlinkSpeed, 0.65f);
        }

        if (!canOpenButtons || buttonsOpening)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            StartCoroutine(OpenButtons());
        }
    }

    private IEnumerator OpenButtons()
    {
        buttonsOpening = true;
        canOpenButtons = false;

        if (clickPrompt != null)
        {
            clickPrompt.gameObject.SetActive(false);
        }

        for (int i = 0; i < menuButtons.Length; i++)
        {
            if (menuButtons[i] == null)
            {
                continue;
            }

            menuButtons[i].gameObject.SetActive(true);
            StartCoroutine(Move(menuButtons[i], menuButtons[i].anchoredPosition, buttonTargets[i], buttonSlideDuration));
            yield return new WaitForSecondsRealtime(buttonStagger);
        }
    }

    private IEnumerator Move(RectTransform target, Vector2 from, Vector2 to, float duration)
    {
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);
            target.anchoredPosition = Vector2.LerpUnclamped(from, to, t);

            if (elapsed >= duration)
            {
                break;
            }

            yield return null;
        }

        target.anchoredPosition = to;
    }

    private void RefreshClickPromptOutline()
    {
        if (clickPromptText == null)
        {
            return;
        }

        EnsureClickPromptOutlineTexts();
        EnsureClickPromptForegroundText();

        for (int i = 0; i < clickPromptOutlineTexts.Length; i++)
        {
            TextMeshProUGUI outline = clickPromptOutlineTexts[i];
            if (outline == null) continue;

            CopyClickPromptTextStyle(clickPromptText, outline);
            outline.text = clickPromptText.text;
            outline.color = clickPromptOutlineColor;
        }

        if (clickPromptForegroundText != null)
        {
            CopyClickPromptTextStyle(clickPromptText, clickPromptForegroundText);
            clickPromptForegroundText.text = clickPromptText.text;
            clickPromptForegroundText.color = clickPromptTextColor;
            clickPromptForegroundText.raycastTarget = false;
            ApplyClickPromptTextRect(clickPromptForegroundText.rectTransform, clickPromptText.rectTransform, Vector2.zero);
            clickPromptForegroundText.transform.SetAsLastSibling();
        }

        Color hiddenSourceColor = clickPromptText.color;
        hiddenSourceColor.a = 0f;
        clickPromptText.color = hiddenSourceColor;
    }

    private void EnsureClickPromptOutlineTexts()
    {
        Transform parent = GetClickPromptOutlineParent();
        RemoveLegacyClickPromptOutlines(parent);
        RectTransform sourceRect = clickPromptText.rectTransform;

        for (int i = 0; i < clickPromptOutlineTexts.Length; i++)
        {
            if (clickPromptOutlineTexts[i] == null)
            {
                Transform existing = parent.Find("ClickPromptOutline_" + i);
                if (existing != null)
                {
                    clickPromptOutlineTexts[i] = existing.GetComponent<TextMeshProUGUI>();
                }
            }

            if (clickPromptOutlineTexts[i] == null)
            {
                GameObject outlineObject = new GameObject("ClickPromptOutline_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(CanvasGroup));
                outlineObject.transform.SetParent(parent, false);
                outlineObject.transform.SetSiblingIndex(clickPromptText.transform.GetSiblingIndex());
                clickPromptOutlineTexts[i] = outlineObject.GetComponent<TextMeshProUGUI>();
            }

            CanvasGroup outlineCanvasGroup = clickPromptOutlineTexts[i].GetComponent<CanvasGroup>();
            if (outlineCanvasGroup == null)
            {
                outlineCanvasGroup = clickPromptOutlineTexts[i].gameObject.AddComponent<CanvasGroup>();
            }
            outlineCanvasGroup.alpha = 1f;
            outlineCanvasGroup.interactable = false;
            outlineCanvasGroup.blocksRaycasts = false;
            outlineCanvasGroup.ignoreParentGroups = true;

            RectTransform outlineRect = clickPromptOutlineTexts[i].rectTransform;
            ApplyClickPromptTextRect(outlineRect, sourceRect, ClickPromptOutlineOffsets[i] * clickPromptOutlineDistance);
            clickPromptOutlineTexts[i].raycastTarget = false;
            clickPromptOutlineTexts[i].transform.SetAsFirstSibling();
        }
    }

    private void EnsureClickPromptForegroundText()
    {
        Transform parent = GetClickPromptOutlineParent();
        if (clickPromptForegroundText == null)
        {
            Transform existing = parent.Find("ClickPromptForeground");
            if (existing != null)
            {
                clickPromptForegroundText = existing.GetComponent<TextMeshProUGUI>();
            }
        }

        if (clickPromptForegroundText == null)
        {
            GameObject foregroundObject = new GameObject("ClickPromptForeground", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            foregroundObject.transform.SetParent(parent, false);
            clickPromptForegroundText = foregroundObject.GetComponent<TextMeshProUGUI>();
        }
    }

    private void ApplyClickPromptTextRect(RectTransform targetRect, RectTransform sourceRect, Vector2 offset)
    {
        bool sourceIsPromptRoot = clickPrompt != null && clickPromptText.transform == clickPrompt.transform;
        if (sourceIsPromptRoot)
        {
            targetRect.anchorMin = Vector2.zero;
            targetRect.anchorMax = Vector2.one;
            targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.sizeDelta = Vector2.zero;
            targetRect.anchoredPosition = offset;
        }
        else
        {
            targetRect.anchorMin = sourceRect.anchorMin;
            targetRect.anchorMax = sourceRect.anchorMax;
            targetRect.pivot = sourceRect.pivot;
            targetRect.sizeDelta = sourceRect.sizeDelta;
            targetRect.anchoredPosition = sourceRect.anchoredPosition + offset;
        }

        targetRect.localRotation = sourceRect.localRotation;
        targetRect.localScale = sourceRect.localScale;
    }

    private Transform GetClickPromptOutlineParent()
    {
        return clickPrompt != null ? clickPrompt.transform : clickPromptText.transform.parent;
    }

    private void RemoveLegacyClickPromptOutlines(Transform desiredParent)
    {
        Transform oldParent = clickPromptText.transform.parent;
        if (oldParent == null || oldParent == desiredParent)
        {
            return;
        }

        for (int i = 0; i < clickPromptOutlineTexts.Length; i++)
        {
            Transform legacy = oldParent.Find("ClickPromptOutline_" + i);
            if (legacy == null)
            {
                continue;
            }

            Destroy(legacy.gameObject);
        }
    }

    private void CopyClickPromptTextStyle(TextMeshProUGUI source, TextMeshProUGUI target)
    {
        target.font = source.font;
        target.fontSharedMaterial = source.fontSharedMaterial;
        target.fontSize = source.fontSize;
        target.fontStyle = source.fontStyle;
        target.alignment = source.alignment;
        target.textWrappingMode = source.textWrappingMode;
        target.overflowMode = source.overflowMode;
        target.richText = source.richText;
        target.characterSpacing = source.characterSpacing;
        target.wordSpacing = source.wordSpacing;
        target.lineSpacing = source.lineSpacing;
        target.enableAutoSizing = source.enableAutoSizing;
        target.fontSizeMin = source.fontSizeMin;
        target.fontSizeMax = source.fontSizeMax;
    }
}
