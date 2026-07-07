using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MissionPanelHoverController : MonoBehaviour
{
    private const string AvailableStateColor = "#b61959";

    [SerializeField] private GameObject dPanel;
    [SerializeField] private Image dPanelTriggerImage;
    [SerializeField] [Range(0f, 1f)] private float dPanelAlphaHitTestMinimumThreshold = 0.1f;
    [SerializeField] [Range(0f, 1f)] private float grayBlend = 0.24f;
    [SerializeField] [Range(0f, 1f)] private float missionButtonHoverDarkenBlend = 0.42f;
    [SerializeField] private float flashDuration = 0.16f;
    [SerializeField] private string lockedMessage = "다음 버전을 기대해주세요!";
    [SerializeField] private AudioSource lockedClickSfxSource;
    [SerializeField] private AudioClip lockedClickClip;
    [SerializeField] private float lockedShakeDuration = 0.24f;
    [SerializeField] private float lockedShakeDistance = 16f;
    [SerializeField] [Range(0f, 1f)] private float lockedClickDarkenBlend = 0.38f;
    [SerializeField] private string firstMissionSceneName = "Idapen";
    [SerializeField] private string secondMissionSceneName = "KarimHasan";
    [SerializeField] private AudioSource missionClickSfxSource;
    [SerializeField] private AudioClip missionClickClip;
    [SerializeField] private float sceneFadeDuration = 0.8f;
    [SerializeField] private float sceneLoadHoldSeconds = 1f;
    [SerializeField] private Vector2 missionScoreLabelOffset = new Vector2(18f, 0f);
    [SerializeField] private Vector2 missionScoreLabelSize = new Vector2(260f, 136f);
    [SerializeField] private float missionScoreFontSize = 22f;
    [SerializeField] private Vector2 missionScoreBackgroundPadding = new Vector2(12f, 8f);
    [SerializeField] [Range(0f, 1f)] private float missionScoreBackgroundAlpha = 0.42f;


    private TextMeshProUGUI lockedMessageText;
    private CanvasGroup lockedMessageGroup;
    private Coroutine lockedMessageRoutine;
    private Coroutine lockedFeedbackRoutine;
    private Image lockedFeedbackImage;
    private Vector2 lockedFeedbackBasePosition;
    private Color lockedFeedbackBaseColor;
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
            ApplyBattleReturnDPanelText();
        }

        RefreshMissionStateTexts();
        Image[] images = GetComponentsInChildren<Image>(true);

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] == null || images[i].gameObject == gameObject)
            {
                continue;
            }

            SetupGrayHover(images[i]);

            if (IsMissionButton(images[i].name) && !IsLockedAtCurrentProgress(images[i].name))
            {
                SetupClickFlash(images[i]);
            }

            if (IsFirstMissionButton(images[i].name))
            {
                SetupMissionClick(images[i], firstMissionSceneName);
            }

            if (IsSecondMissionButton(images[i].name) && IsSecondMissionUnlocked())
            {
                SetupMissionClick(images[i], secondMissionSceneName);
            }

            if (IsLockedAtCurrentProgress(images[i].name))
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
        PostVictoryMainDialogueController.PlayPendingIfAny(transform.root);
    }

private void OnEnable()
    {
        RefreshMissionStateTexts();
        EnsureMissionScoreLabels();
    }


    private void SetupGrayHover(Image image)
    {
        image.raycastTarget = true;
        ConfigureAlphaHitTest(image);
        EventTrigger trigger = image.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        Color originalColor = image.color;
        RemovePointerEvents(trigger, false);
        AddPointerEvent(trigger, EventTriggerType.PointerEnter, () =>
        {
            float blend = IsMissionButton(image.name) ? missionButtonHoverDarkenBlend : grayBlend;
            image.color = Color.Lerp(originalColor, Color.black, blend);
        });
        AddPointerEvent(trigger, EventTriggerType.PointerExit, () => image.color = originalColor);
    }

    private void SetupDPanelTrigger(Image image)
    {
        image.raycastTarget = true;
        ConfigureAlphaHitTest(image);
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

        bool pointerStillOnOpaquePixel =
            RectTransformUtility.RectangleContainsScreenPoint(image.rectTransform, Input.mousePosition, eventCamera)
            && image.IsRaycastLocationValid(Input.mousePosition, eventCamera);

        if (!pointerStillOnOpaquePixel)
        {
            SetDPanelVisible(false);
        }
    }

    private void ConfigureAlphaHitTest(Image image)
    {
        if (!CanSampleSpriteAlpha(image))
        {
            return;
        }

        image.alphaHitTestMinimumThreshold = Mathf.Clamp01(dPanelAlphaHitTestMinimumThreshold);
    }

    private bool CanSampleSpriteAlpha(Image image)
    {
        return image != null
            && image.sprite != null
            && image.sprite.texture != null
            && image.sprite.texture.isReadable;
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

        AddPointerEvent(trigger, EventTriggerType.PointerClick, () => HandleLockedClick(image));
    }

    private void HandleLockedClick(Image image)
    {
        PlayLockedClickSfx();
        ShowLockedMessage();

        if (lockedFeedbackRoutine != null)
        {
            StopCoroutine(lockedFeedbackRoutine);
            RestoreLockedFeedbackTarget();
        }

        lockedFeedbackRoutine = StartCoroutine(PlayLockedClickFeedback(image));
    }

    private void PlayLockedClickSfx()
    {
        UIClickSoundPlayer.Play(gameObject, ref lockedClickSfxSource, lockedClickClip);
    }

    private void RestoreLockedFeedbackTarget()
    {
        if (lockedFeedbackImage == null)
        {
            return;
        }

        lockedFeedbackImage.color = lockedFeedbackBaseColor;

        RectTransform rectTransform = lockedFeedbackImage.rectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = lockedFeedbackBasePosition;
        }
    }

    private void SetupMissionClick(Image image, string sceneName)
    {
        EventTrigger trigger = image.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = image.gameObject.AddComponent<EventTrigger>();
        }

        AddPointerEvent(trigger, EventTriggerType.PointerClick, () => StartMissionTransition(sceneName));
    }

    private bool IsFirstMissionButton(string objectName)
    {
        return objectName == "MButton" || objectName == "MButton1";
    }

    private bool IsSecondMissionButton(string objectName)
    {
        return objectName == "MButton2" || objectName == "MButton (1)";
    }

    private bool IsLockedButton(string objectName)
    {
        return objectName == "MButton2"
            || objectName == "MButton3"
            || objectName == "MButton (1)"
            || objectName == "MButton (2)";
    }

    private bool IsLockedAtCurrentProgress(string objectName)
    {
        if (IsSecondMissionButton(objectName))
        {
            return !IsSecondMissionUnlocked();
        }

        return IsLockedButton(objectName);
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

    private IEnumerator PlayLockedClickFeedback(Image image)
    {
        if (image == null)
        {
            yield break;
        }

        lockedFeedbackImage = image;
        RectTransform rectTransform = image.rectTransform;
        lockedFeedbackBasePosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        lockedFeedbackBaseColor = image.color;

        Color darkColor = Color.Lerp(lockedFeedbackBaseColor, Color.black, lockedClickDarkenBlend);
        yield return LerpImageColor(image, lockedFeedbackBaseColor, darkColor, 0.04f);

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            if (image == null || rectTransform == null)
            {
                yield break;
            }

            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = lockedShakeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / lockedShakeDuration);
            float dampedShake = Mathf.Sin(t * Mathf.PI * 8f) * lockedShakeDistance * (1f - t);
            rectTransform.anchoredPosition = lockedFeedbackBasePosition + new Vector2(dampedShake, 0f);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = lockedFeedbackBasePosition;
        }

        yield return LerpImageColor(image, darkColor, lockedFeedbackBaseColor, 0.08f);
        lockedFeedbackRoutine = null;
        lockedFeedbackImage = null;
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

    private void ApplyBattleReturnDPanelText()
    {
        bool defeated = PlayerPrefs.GetInt("BattleDefeat", 0) == 1;
        bool victorious = PlayerPrefs.GetInt("BattleVictory", 0) == 1;

        if (!defeated && !victorious)
        {
            return;
        }

        string message = defeated ? "실망이군요." : "잘했어요.";
        PlayerPrefs.DeleteKey("BattleDefeat");
        PlayerPrefs.DeleteKey("BattleVictory");
        PlayerPrefs.Save();

        TextMeshProUGUI[] texts = dPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            texts[i].text = message;
            texts[i].overflowMode = TMPro.TextOverflowModes.Truncate;
            texts[i].textWrappingMode = TMPro.TextWrappingModes.Normal;
            texts[i].enableAutoSizing = false;
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

    private void StartMissionTransition(string sceneName)
    {
        if (sceneTransitionStarted)
        {
            return;
        }

        sceneTransitionStarted = true;
        PlayMissionClickSfx();
        StartCoroutine(LoadMissionScene(sceneName));
    }

    private IEnumerator LoadMissionScene(string sceneName)
    {
        Image fadeImage = CreateSceneFadeImage();
        yield return FadeImage(fadeImage, 0f, 1f, sceneFadeDuration);
        yield return new WaitForSecondsRealtime(sceneLoadHoldSeconds);

        if (!string.IsNullOrWhiteSpace(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
    }

    private void PlayMissionClickSfx()
    {
        UIClickSoundPlayer.Play(gameObject, ref missionClickSfxSource, missionClickClip != null ? missionClickClip : lockedClickClip);
    }

    private Image CreateSceneFadeImage()
    {
        return SceneFadeOverlay.CreateImage("MissionSceneFade");
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
            label.text = $"현재 점수: {currentScore} / 최고 점수: {bestScore}";
            FitMissionScoreBackground(label);
        }
    }

    private void RefreshMissionStateTexts()
    {
        Image[] images = GetComponentsInChildren<Image>(true);
        for (int i = 0; i < images.Length; i++)
        {
            Image image = images[i];
            if (image == null || !IsMissionButton(image.name))
            {
                continue;
            }

            TextMeshProUGUI title = GetMissionTitleText(image.transform);
            if (title == null)
            {
                continue;
            }

            title.richText = true;
            string missionId = GetMissionId(image.name);
            bool completed = IsMissionCompleted(missionId);

            if (IsFirstMissionButton(image.name))
            {
                title.text = completed
                    ? "\u2460 \uC774\uB2E4 \uD39C .................. [\uCC98\uB9AC \uC644\uB8CC]"
                    : $"\u2460 \uC774\uB2E4 \uD39C .................. <color={AvailableStateColor}>[\uCC98\uB9AC \uAC00\uB2A5]</color>";
                continue;
            }

            if (IsSecondMissionButton(image.name))
            {
                if (completed)
                {
                    title.text = "\u2461 \uCE74\uB9BC \uD558\uC0B0 ............... [\uCC98\uB9AC \uC644\uB8CC]";
                }
                else if (IsSecondMissionUnlocked())
                {
                    title.text = $"\u2461 \uCE74\uB9BC \uD558\uC0B0 ............... <color={AvailableStateColor}>[\uCC98\uB9AC \uAC00\uB2A5]</color>";
                }
                else
                {
                    title.text = "\u2461 \uCE74\uB9BC \uD558\uC0B0 ............... [\uC7A0\uAE08]";
                }
            }
        }
    }

    private TextMeshProUGUI GetMissionTitleText(Transform missionButton)
    {
        if (missionButton == null)
        {
            return null;
        }

        TextMeshProUGUI[] texts = missionButton.GetComponentsInChildren<TextMeshProUGUI>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TextMeshProUGUI text = texts[i];
            if (text != null && text.gameObject.name != "MissionScoreText")
            {
                return text;
            }
        }

        return null;
    }

    private bool IsMissionCompleted(string missionId)
    {
        return BattleScoreStore.GetBestScore(missionId) > 0
            || BattleScoreStore.GetCurrentScore(missionId) > 0;
    }

    private bool IsSecondMissionUnlocked()
    {
        return IsMissionCompleted(BattleScoreStore.DefaultMissionId);
    }

    private TextMeshProUGUI GetOrCreateMissionScoreLabel(RectTransform buttonTransform)
    {
        const string labelName = "MissionScoreText";
        Transform existing = buttonTransform.Find(labelName);
        TextMeshProUGUI label;
        bool createdLabel = false;

        if (existing != null)
        {
            label = existing.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            GameObject labelObject = new GameObject(labelName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(buttonTransform, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
            createdLabel = true;
        }

        Image background = GetOrCreateMissionScoreBackground(buttonTransform);

        RectTransform labelTransform = label.rectTransform;
        if (createdLabel)
        {
            labelTransform.anchorMin = new Vector2(1f, 0.5f);
            labelTransform.anchorMax = new Vector2(1f, 0.5f);
            labelTransform.pivot = new Vector2(0f, 0.5f);
            labelTransform.anchoredPosition = missionScoreLabelOffset;
            labelTransform.sizeDelta = missionScoreLabelSize;
        }

        background.color = new Color(0f, 0f, 0f, missionScoreBackgroundAlpha);
        background.raycastTarget = false;
        background.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
        label.transform.SetAsLastSibling();

        if (createdLabel && TMP_Settings.defaultFontAsset != null)
        {
            label.font = TMP_Settings.defaultFontAsset;
        }

        if (createdLabel)
        {
            label.fontSize = missionScoreFontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }

        label.raycastTarget = false;
        return label;
    }

    private void FitMissionScoreBackground(TextMeshProUGUI label)
    {
        if (label == null)
        {
            return;
        }

        RectTransform buttonTransform = label.rectTransform.parent as RectTransform;
        Image background = GetOrCreateMissionScoreBackground(buttonTransform);
        if (background == null)
        {
            return;
        }

        label.ForceMeshUpdate();
        Vector2 preferredSize = label.GetPreferredValues(label.text);
        Vector2 backgroundSize = new Vector2(
            Mathf.Max(1f, preferredSize.x + missionScoreBackgroundPadding.x),
            Mathf.Max(1f, preferredSize.y + missionScoreBackgroundPadding.y));

        RectTransform labelTransform = label.rectTransform;
        RectTransform backgroundTransform = background.rectTransform;
        backgroundTransform.anchorMin = labelTransform.anchorMin;
        backgroundTransform.anchorMax = labelTransform.anchorMax;
        backgroundTransform.pivot = labelTransform.pivot;
        backgroundTransform.anchoredPosition = labelTransform.anchoredPosition - missionScoreBackgroundPadding * 0.5f;
        backgroundTransform.sizeDelta = backgroundSize;
        background.color = new Color(0f, 0f, 0f, missionScoreBackgroundAlpha);
        background.raycastTarget = false;
        background.transform.SetSiblingIndex(label.transform.GetSiblingIndex());
        label.transform.SetAsLastSibling();
    }

    private Image GetOrCreateMissionScoreBackground(RectTransform buttonTransform)
    {
        if (buttonTransform == null)
        {
            return null;
        }

        const string backgroundName = "MissionScoreBackground";
        Transform existing = buttonTransform.Find(backgroundName);
        Image background;

        if (existing != null)
        {
            background = existing.GetComponent<Image>();
            if (background != null)
            {
                return background;
            }
        }

        GameObject backgroundObject = new GameObject(backgroundName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        backgroundObject.transform.SetParent(buttonTransform, false);
        background = backgroundObject.GetComponent<Image>();
        return background;
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
