using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MissionPanelHoverController : MonoBehaviour
{
    [SerializeField] private GameObject dPanel;
    [SerializeField] private Image dPanelTriggerImage;
    [SerializeField] [Range(0f, 1f)] private float grayBlend = 0.24f;
    [SerializeField] private float flashDuration = 0.16f;
    [SerializeField] private string lockedMessage = "잠금 해제되지 않았습니다!";
    [SerializeField] private string firstMissionSceneName = "Idapen";
    [SerializeField] private float sceneFadeDuration = 0.8f;
    [SerializeField] private float sceneLoadHoldSeconds = 1f;
    [SerializeField] private Vector2 missionScoreLabelOffset = new Vector2(18f, 0f);
    [SerializeField] private Vector2 missionScoreLabelSize = new Vector2(260f, 136f);
    [SerializeField] private float missionScoreFontSize = 22f;


    private TextMeshProUGUI lockedMessageText;
    private CanvasGroup lockedMessageGroup;
    private Coroutine lockedMessageRoutine;
    private bool sceneTransitionStarted;

    private void Awake()
    {
        if (dPanel == null)
        {
            Transform panel = transform.root.Find("Canvas1/DPanel");
            if (panel != null)
            {
                dPanel = panel.gameObject;
            }
        }

        if (dPanel != null)
        {
            dPanel.SetActive(false);
            DisableDPanelRaycasts();
        }

        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i].gameObject == gameObject)
            {
                continue;
            }

            SetupGrayHover(images[i]);

            if (IsMissionButton(images[i].name))
            {
                SetupClickFlash(images[i]);
            }

            if (IsFirstMissionButton(images[i].name))
            {
                SetupFirstMissionClick(images[i]);
            }

            if (IsLockedButton(images[i].name))
            {
                SetupLockedClick(images[i]);
            }
        }

        if (dPanelTriggerImage == null)
        {
            Transform trigger = transform.Find("Image");
            if (trigger != null)
            {
                dPanelTriggerImage = trigger.GetComponent<Image>();
            }
        }

        if (dPanelTriggerImage != null)
        {
            SetupDPanelTrigger(dPanelTriggerImage);
        }

        EnsureLockedMessage();
    }

private void OnEnable()
    {
        EnsureMissionScoreLabels();
    }


    private void SetupGrayHover(Image image)
    {
        image.raycastTarget = true;
        EventTrigger trigger = image.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        Color originalColor = image.color;
        RemovePointerEvents(trigger, false);
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => image.color = Color.Lerp(originalColor, Color.gray, grayBlend));
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => image.color = originalColor);
    }

    private void SetupDPanelTrigger(Image image)
    {
        image.raycastTarget = true;
        EventTrigger trigger = image.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        RemovePointerEvents(trigger, true);
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () => SetDPanelVisible(true));
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => StartCoroutine(HideDPanelAfterPointerCheck(image)));
    }

    private void SetDPanelVisible(bool visible)
    {
        if (dPanel != null)
        {
            dPanel.SetActive(visible);
            if (visible)
            {
                dPanel.transform.SetAsLastSibling();
            }
        }
    }

    private IEnumerator HideDPanelAfterPointerCheck(Image image)
    {
        yield return null;

        if (image == null)
        {
            SetDPanelVisible(false);
            yield break;
        }

        Camera eventCamera = null;
        Canvas canvas = image.canvas;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            eventCamera = canvas.worldCamera;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, Input.mousePosition, eventCamera))
        {
            SetDPanelVisible(false);
        }
    }

    private void DisableDPanelRaycasts()
    {
        Graphic[] graphics = dPanel.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }

        CanvasGroup group = dPanel.GetComponent<CanvasGroup>();
        if (group == null)
        {
            group = dPanel.AddComponent<CanvasGroup>();
        }

        group.blocksRaycasts = false;
        group.interactable = false;
    }

    private void SetupClickFlash(Image image)
    {
        EventTrigger trigger = image.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        AddPointerEvent(trigger, EventTriggerType.PointerClick, () => StartCoroutine(FlashImage(image)));
    }

    private void SetupLockedClick(Image image)
    {
        EventTrigger trigger = image.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        AddPointerEvent(trigger, EventTriggerType.PointerClick, ShowLockedMessage);
    }

    private void SetupFirstMissionClick(Image image)
    {
        EventTrigger trigger = image.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        AddPointerEvent(trigger, EventTriggerType.PointerClick, StartFirstMissionTransition);
    }

    private bool IsFirstMissionButton(string objectName)
    {
        return objectName == "MButton" || objectName == "MButton1";
    }

    private bool IsLockedButton(string objectName)
    {
        return objectName == "MButton2"
            || objectName == "MButton3"
            || objectName == "MButton (1)"
            || objectName == "MButton (2)";
    }

    private bool IsMissionButton(string objectName)
    {
        return objectName == "MButton"
            || objectName == "MButton1"
            || objectName == "MButton2"
            || objectName == "MButton3"
            || objectName == "MButton (1)"
            || objectName == "MButton (2)";
    }

    private IEnumerator FlashImage(Image image)
    {
        if (image == null)
        {
            yield break;
        }

        Color original = image.color;
        Color flash = Color.Lerp(original, Color.white, 0.65f);
        float halfDuration = Mathf.Max(0.01f, flashDuration * 0.5f);

        yield return LerpImageColor(image, original, flash, halfDuration);
        yield return LerpImageColor(image, flash, original, halfDuration);
    }

    private IEnumerator LerpImageColor(Image image, Color from, Color to, float duration)
    {
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            if (image == null)
            {
                yield break;
            }

            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            image.color = Color.Lerp(from, to, t);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }

    private void EnsureLockedMessage()
    {
        if (lockedMessageText != null)
        {
            return;
        }

        Transform parent = transform.root;
        GameObject messageObject = new GameObject("LockedMissionMessage", typeof(RectTransform), typeof(CanvasRenderer), typeof(CanvasGroup), typeof(TextMeshProUGUI));
        messageObject.transform.SetParent(parent, false);
        messageObject.transform.SetAsLastSibling();

        RectTransform rectTransform = messageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(1f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(1f, 1f);
        rectTransform.anchoredPosition = new Vector2(-32f, -28f);
        rectTransform.sizeDelta = new Vector2(520f, 70f);

        lockedMessageGroup = messageObject.GetComponent<CanvasGroup>();
        lockedMessageGroup.alpha = 0f;
        lockedMessageGroup.interactable = false;
        lockedMessageGroup.blocksRaycasts = false;

        lockedMessageText = messageObject.GetComponent<TextMeshProUGUI>();
        lockedMessageText.text = lockedMessage;
        lockedMessageText.fontSize = 30f;
        lockedMessageText.color = Color.white;
        lockedMessageText.alignment = TextAlignmentOptions.TopRight;
        lockedMessageText.raycastTarget = false;
    }

    private void ShowLockedMessage()
    {
        EnsureLockedMessage();
        EnsureMissionScoreLabels();


        if (lockedMessageRoutine != null)
        {
            StopCoroutine(lockedMessageRoutine);
        }

        lockedMessageRoutine = StartCoroutine(ShowLockedMessageRoutine());
    }

    private void StartFirstMissionTransition()
    {
        if (sceneTransitionStarted)
        {
            return;
        }

        sceneTransitionStarted = true;
        StartCoroutine(LoadFirstMissionScene());
    }

    private IEnumerator LoadFirstMissionScene()
    {
        Image fadeImage = CreateSceneFadeImage();
        yield return FadeImage(fadeImage, 0f, 1f, sceneFadeDuration);
        yield return new WaitForSecondsRealtime(sceneLoadHoldSeconds);

        if (!string.IsNullOrWhiteSpace(firstMissionSceneName))
        {
            SceneManager.LoadScene(firstMissionSceneName);
        }
    }

    private Image CreateSceneFadeImage()
    {
        Transform parent = transform.root;
        GameObject fadeObject = new GameObject("MissionSceneFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeObject.transform.SetParent(parent, false);
        fadeObject.transform.SetAsLastSibling();

        RectTransform rectTransform = fadeObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image fadeImage = fadeObject.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = true;
        return fadeImage;
    }

    private IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        if (image == null)
        {
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            image.color = new Color(0f, 0f, 0f, Mathf.Lerp(from, to, t));

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }

    private IEnumerator ShowLockedMessageRoutine()
    {
        lockedMessageText.transform.SetAsLastSibling();
        yield return FadeLockedMessage(0f, 1f, 0.18f);
        yield return new WaitForSecondsRealtime(2f);
        yield return FadeLockedMessage(1f, 0f, 0.25f);
        lockedMessageRoutine = null;
    }

    private IEnumerator FadeLockedMessage(float from, float to, float duration)
    {
        if (lockedMessageGroup == null)
        {
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            lockedMessageGroup.alpha = Mathf.Lerp(from, to, t);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }

    private void RemovePointerEvents(EventTrigger trigger, bool dPanelOnly)
    {
        trigger.triggers.RemoveAll(entry =>
            entry.eventID == EventTriggerType.PointerEnter || entry.eventID == EventTriggerType.PointerExit);
    }

    private void AddPointerEvent(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction callback)
    {
        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = eventType };
        entry.callback.AddListener(_ => callback());
        trigger.triggers.Add(entry);
    }


private void EnsureMissionScoreLabels()
    {
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || !IsMissionButton(image.name))
            {
                continue;
            }

            TextMeshProUGUI label = GetOrCreateMissionScoreLabel(image.rectTransform);
            string missionId = GetMissionId(image.name);
            int currentScore = BattleScoreStore.GetCurrentScore(missionId);
            int bestScore = BattleScoreStore.GetBestScore(missionId);
            label.text = $"현재점수: {currentScore}\n최고점수: {bestScore}";
        }
    }

    private TextMeshProUGUI GetOrCreateMissionScoreLabel(RectTransform buttonTransform)
    {
        const string labelName = "MissionScoreText";
        Transform existing = buttonTransform.Find(labelName);
        TextMeshProUGUI label;

        if (existing != null)
        {
            label = existing.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject labelObject = new GameObject(labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonTransform, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }

        RectTransform labelTransform = label.rectTransform;
        labelTransform.anchorMin = new Vector2(1f, 0.5f);
        labelTransform.anchorMax = new Vector2(1f, 0.5f);
        labelTransform.pivot = new Vector2(0f, 0.5f);
        labelTransform.anchoredPosition = missionScoreLabelOffset;
        labelTransform.sizeDelta = missionScoreLabelSize;

        if (TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        label.fontSize = missionScoreFontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        return label;
    }

    private string GetMissionId(string objectName)
    {
        if (objectName == "MButton" || objectName == "MButton1")
        {
            return BattleScoreStore.DefaultMissionId;
        }

        if (objectName == "MButton2" || objectName == "MButton (1)")
        {
            return "Mission2";
        }

        return "Mission3";
    }
}
