using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[ExecuteAlways]
public class CardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public CardData cardData;
    public BattleManager battleManager;

    public TMP_Text cardNameText;

    [Header("Card Image")]
    
    [SerializeField] [Range(0f, 1f)] private float alphaHitTestMinimumThreshold = 0.1f;
[SerializeField] private Image cardImage;
    [SerializeField] private Sprite dealImage;
    [SerializeField] private Sprite houseImage;
    [SerializeField] private Sprite gambleImage;
    [SerializeField] private Sprite povertyImage;
    [SerializeField] private Sprite negotiationImage;
    [SerializeField] private Sprite manipulationImage;
    [SerializeField] private Sprite fluxImage;
    [SerializeField] private Sprite barrierImage;
    [SerializeField] private Color nameOutlineColor = new Color(0f, 0f, 0f, 1f);
    [SerializeField] private float nameOutlineDistance = 3.15f;
    [SerializeField] private Color cardOutlineColor = new Color(0.08f, 0.03f, 0.07f, 0.82f);
    [SerializeField] private Vector2 cardOutlineDistance = new Vector2(4f, -4f);
    [SerializeField] [Range(0f, 1f)] private float hoverDarkenAmount = 0.28f;
    [SerializeField] private float hoverScale = 1.085f;
    [SerializeField] private float hoverLift = 15f;
    [SerializeField] private float hoverTweenDuration = 0.12f;

    private static readonly Vector2[] NameOutlineOffsets =
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

    private readonly TextMeshProUGUI[] nameOutlineTexts = new TextMeshProUGUI[NameOutlineOffsets.Length];
    private Coroutine clickFeedbackRoutine;
    private Coroutine hoverFeedbackRoutine;
    private Coroutine returnDragRoutine;
    private Vector3 clickFeedbackBaseScale = Vector3.one;
    private Vector2 clickFeedbackBasePosition;
    private Color clickFeedbackBaseColor = Color.white;
    private Vector3 hoverBaseScale = Vector3.one;
    private Color hoverBaseColor = Color.white;
    private Vector3 visualBaseScale = Vector3.one;
    private Vector2 visualBasePosition;
    private Color visualBaseColor = Color.white;
    private bool hovering;
    private bool dragging;
    private bool dragAllowed;
    private bool suppressNextClick;
    private Transform dragOriginalParent;
    private int dragOriginalSiblingIndex;
    private Vector2 dragOriginalAnchoredPosition;
    private Vector3 dragOriginalScale;
    private Canvas dragCanvas;
    private RectTransform dragCanvasRect;
    private LayoutElement dragLayoutElement;
    private Vector2 dragOriginalAnchorMin;
    private Vector2 dragOriginalAnchorMax;
    private Vector2 dragOriginalPivot;
    private Vector2 dragOriginalSize;
    private GameObject dragPlaceholder;
    private SmoothHandLayoutAnimator handLayoutAnimator;
#if UNITY_EDITOR
    private bool nameOutlineRefreshQueued;
#endif

    private void Awake()
    {
        EnsureImageReference();
        if (Application.isPlaying)
        {
            EnsureCardOutline();
        }
        RefreshNameTextOutlineSafely();
    }

    private void OnValidate()
    {
        EnsureImageReference();
    }

    private void Start()
    {
        if (cardData != null)
        {
            RefreshCardUI();
        }
    }

    public void RefreshCardUI()
    {
        if (cardData == null)
        {
            Debug.LogWarning("CardData가 비어 있습니다.", gameObject);
            return;
        }

        if (cardNameText == null)
        {
            Debug.LogWarning("CardNameText가 연결되지 않았습니다.", gameObject);
            return;
        }

        cardNameText.richText = true;
        cardNameText.text = battleManager != null
            ? battleManager.GetCardButtonLabel(cardData)
            : cardData.cardName;

        ApplyNameTextOutline();
        ApplyCardImage();
        EnsureCardOutline();
        CaptureVisualBaseState();
    }

    private void ApplyNameTextOutline()
    {
        if (cardNameText == null)
        {
            return;
        }

        TextMeshProUGUI source = cardNameText as TextMeshProUGUI;
        if (source == null)
        {
            return;
        }

        RemoveUnusedNameOutlineTexts(source.transform.parent);
        EnsureNameOutlineTexts(source);

        for (int i = 0; i < nameOutlineTexts.Length; i++)
        {
            TextMeshProUGUI outline = nameOutlineTexts[i];
            if (outline == null) continue;

            CopyNameTextStyle(source, outline);
            outline.text = source.text;
            outline.color = nameOutlineColor;
        }

        source.transform.SetAsLastSibling();
    }

    private void RefreshNameTextOutlineSafely()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            QueueNameOutlineRefresh();
            return;
        }
#endif
        ApplyNameTextOutline();
    }

#if UNITY_EDITOR
    private void QueueNameOutlineRefresh()
    {
        if (nameOutlineRefreshQueued)
        {
            return;
        }

        nameOutlineRefreshQueued = true;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            nameOutlineRefreshQueued = false;
            if (this == null)
            {
                return;
            }

            ApplyNameTextOutline();
        };
    }
#endif

    private void EnsureNameOutlineTexts(TextMeshProUGUI source)
    {
        Transform parent = source.transform.parent;
        for (int i = 0; i < nameOutlineTexts.Length; i++)
        {
            Vector2 offset = NameOutlineOffsets[i];

            if (nameOutlineTexts[i] == null)
            {
                Transform existing = parent.Find("CardNameTextOutline_" + i);
                if (existing != null)
                {
                    nameOutlineTexts[i] = existing.GetComponent<TextMeshProUGUI>();
                }
            }

            if (nameOutlineTexts[i] == null)
            {
                GameObject outlineObject = new GameObject("CardNameTextOutline_" + i, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                outlineObject.transform.SetParent(parent, false);
                outlineObject.transform.SetSiblingIndex(source.transform.GetSiblingIndex());
                nameOutlineTexts[i] = outlineObject.GetComponent<TextMeshProUGUI>();
            }

            RectTransform sourceRect = source.rectTransform;
            RectTransform outlineRect = nameOutlineTexts[i].rectTransform;
            outlineRect.anchorMin = sourceRect.anchorMin;
            outlineRect.anchorMax = sourceRect.anchorMax;
            outlineRect.pivot = sourceRect.pivot;
            outlineRect.sizeDelta = sourceRect.sizeDelta;
            outlineRect.anchoredPosition = sourceRect.anchoredPosition + offset * nameOutlineDistance;
            outlineRect.localRotation = sourceRect.localRotation;
            outlineRect.localScale = sourceRect.localScale;
            nameOutlineTexts[i].raycastTarget = false;
            nameOutlineTexts[i].transform.SetSiblingIndex(source.transform.GetSiblingIndex());
        }
    }

    private void RemoveUnusedNameOutlineTexts(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = nameOutlineTexts.Length; i < 24; i++)
        {
            Transform extra = parent.Find("CardNameTextOutline_" + i);
            if (extra == null)
            {
                continue;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(extra.gameObject);
            }
            else
#endif
            {
                Destroy(extra.gameObject);
            }
        }
    }

    private void CopyNameTextStyle(TextMeshProUGUI source, TextMeshProUGUI target)
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

    private void ApplyCardImage()
    {
        EnsureImageReference();

        if (cardImage == null || cardData == null)
        {
            return;
        }

        Sprite sprite = cardData.cardImage != null ? cardData.cardImage : GetTypeImage(cardData.cardType);
        if (sprite != null)
        {
            cardImage.sprite = sprite;
        }
    }

    private void CaptureVisualBaseState()
    {
        visualBaseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            visualBasePosition = rectTransform.anchoredPosition;
        }

        Image image = GetComponent<Image>();
        if (image != null)
        {
            visualBaseColor = image.color;
        }
    }

    private void EnsureImageReference()
    {
        if (cardImage == null)
        {
            cardImage = GetComponent<Image>();
        }

        if (cardImage != null)
        {
            cardImage.raycastTarget = true;
            try
            {
                cardImage.alphaHitTestMinimumThreshold = alphaHitTestMinimumThreshold;
            }
            catch (System.InvalidOperationException)
            {
                // The sprite texture import settings are fixed separately; avoid repeated editor exceptions meanwhile.
            }
        }
    }

    private void EnsureCardOutline()
    {
        if (cardImage == null)
        {
            return;
        }

        Outline outline = cardImage.GetComponent<Outline>();
        if (outline == null)
        {
            outline = cardImage.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = cardOutlineColor;
        outline.effectDistance = cardOutlineDistance;
        outline.useGraphicAlpha = true;
    }

    private Sprite GetTypeImage(CardType cardType)
    {
        switch (cardType)
        {
            case CardType.Deal:
                return dealImage;
            case CardType.House:
                return houseImage;
            case CardType.Gamble:
                return gambleImage;
            case CardType.Poverty:
                return povertyImage;
            case CardType.Negotiation:
                return negotiationImage;
            case CardType.Manipulation:
                return manipulationImage;
            case CardType.Flux:
                return fluxImage;
            case CardType.Barrier:
                return barrierImage;
            default:
                return null;
        }
    }

public void OnClickCard()
    {
        if (battleManager == null)
        {
            return;
        }

        if (cardData == null)
        {
            Debug.LogWarning("CardData is not connected.", gameObject);
            return;
        }

        if (suppressNextClick)
        {
            suppressNextClick = false;
            return;
        }

        if (!battleManager.CanAcceptCardClick(cardData, out bool insufficientLux))
        {
            if (insufficientLux)
            {
                PlayInsufficientLuxFeedback();
            }
            else
            {
                PlayClickBoing();
            }
            return;
        }

        battleManager.PlayCardSelectSound();
        PlayClickBoing();
    }

    private void PlayClickBoing()
    {
        StartClickFeedback(ClickBoingRoutine());
    }

public void OnBeginDrag(PointerEventData eventData)
    {
        if (!Application.isPlaying || battleManager == null || cardData == null)
        {
            return;
        }

        suppressNextClick = true;
        if (!battleManager.CanAcceptCardClick(cardData, out bool insufficientLux))
        {
            if (insufficientLux)
            {
                PlayInsufficientLuxFeedback();
            }
            else
            {
                PlayClickBoing();
            }
            return;
        }

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        dragCanvas = GetComponentInParent<Canvas>();
        if (dragCanvas == null)
        {
            return;
        }

        dragCanvasRect = dragCanvas.transform as RectTransform;
        if (dragCanvasRect == null)
        {
            return;
        }

        if (hoverFeedbackRoutine != null)
        {
            StopCoroutine(hoverFeedbackRoutine);
            hoverFeedbackRoutine = null;
        }

        if (returnDragRoutine != null)
        {
            StopCoroutine(returnDragRoutine);
            returnDragRoutine = null;
        }

        dragAllowed = true;
        dragging = true;
        hovering = false;
        dragOriginalParent = transform.parent;
        dragOriginalSiblingIndex = transform.GetSiblingIndex();
        dragOriginalAnchoredPosition = visualBasePosition;
        dragOriginalScale = visualBaseScale;
        dragOriginalAnchorMin = rectTransform.anchorMin;
        dragOriginalAnchorMax = rectTransform.anchorMax;
        dragOriginalPivot = rectTransform.pivot;
        dragOriginalSize = rectTransform.rect.size;
        handLayoutAnimator = dragOriginalParent != null ? dragOriginalParent.GetComponent<SmoothHandLayoutAnimator>() : null;
        if (handLayoutAnimator != null)
        {
            handLayoutAnimator.CaptureLayout();
        }

        dragLayoutElement = GetComponent<LayoutElement>();
        if (dragLayoutElement != null)
        {
            dragLayoutElement.ignoreLayout = true;
        }

        transform.localScale = visualBaseScale;
        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.color = visualBaseColor;
        }

        battleManager.HideCardTooltip();
        battleManager.PlayCardDragSound();
        transform.SetParent(dragCanvas.transform, true);
        transform.SetAsLastSibling();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = dragOriginalPivot;
        SetDraggedCardScreenPosition(eventData);
        if (handLayoutAnimator != null)
        {
            handLayoutAnimator.AnimateLayoutShift();
        }
        battleManager.UpdateDraggedCardTargetPreview(cardData, eventData.position);
    }

public void OnDrag(PointerEventData eventData)
    {
        if (!dragAllowed || !dragging)
        {
            return;
        }

        SetDraggedCardScreenPosition(eventData);
        bool overHand = battleManager != null && battleManager.IsScreenPointOverHand(eventData.position);
        UpdateDragPlaceholder(overHand, eventData.position);

        if (battleManager != null && cardData != null)
        {
            if (overHand)
            {
                battleManager.ClearDraggedCardPreview();
            }
            else
            {
                battleManager.UpdateDraggedCardTargetPreview(cardData, eventData.position);
            }
        }
    }

public void OnEndDrag(PointerEventData eventData)
    {
        if (!dragAllowed)
        {
            return;
        }

        dragAllowed = false;
        dragging = false;
        suppressNextClick = true;

        bool overHand = battleManager != null && battleManager.IsScreenPointOverHand(eventData.position);
        bool used = !overHand
            && battleManager != null
            && cardData != null
            && battleManager.TryUseDraggedCardOnEnemy(cardData, eventData.position);

        if (used)
        {
            RemoveDragPlaceholder(true);
            if (dragLayoutElement != null)
            {
                dragLayoutElement.ignoreLayout = false;
            }
            Destroy(gameObject);
            return;
        }

        if (battleManager != null)
        {
            battleManager.ClearDraggedCardPreview();
        }

        ReturnDraggedCardToHand();
    }

    private void SetDraggedCardScreenPosition(PointerEventData eventData)
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null || dragCanvasRect == null)
        {
            return;
        }

        Camera camera = dragCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : eventData.pressEventCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(dragCanvasRect, eventData.position, camera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }

private void ReturnDraggedCardToHand()
    {
        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform == null)
        {
            return;
        }

        if (clickFeedbackRoutine != null)
        {
            StopCoroutine(clickFeedbackRoutine);
            clickFeedbackRoutine = null;
        }

        if (returnDragRoutine != null)
        {
            StopCoroutine(returnDragRoutine);
            returnDragRoutine = null;
        }

        int targetSiblingIndex = dragPlaceholder != null && dragPlaceholder.transform.parent == dragOriginalParent
            ? dragPlaceholder.transform.GetSiblingIndex()
            : dragOriginalSiblingIndex;

        if (handLayoutAnimator != null)
        {
            handLayoutAnimator.CaptureLayout();
        }

        if (dragPlaceholder != null)
        {
            Destroy(dragPlaceholder);
            dragPlaceholder = null;
        }

        if (dragOriginalParent != null)
        {
            transform.SetParent(dragOriginalParent, false);
            transform.SetSiblingIndex(Mathf.Clamp(targetSiblingIndex, 0, dragOriginalParent.childCount - 1));
        }

        rectTransform.anchorMin = dragOriginalAnchorMin;
        rectTransform.anchorMax = dragOriginalAnchorMax;
        rectTransform.pivot = dragOriginalPivot;
        rectTransform.anchoredPosition = dragOriginalAnchoredPosition;
        transform.localScale = dragOriginalScale;

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.color = visualBaseColor;
        }

        if (dragLayoutElement != null)
        {
            dragLayoutElement.ignoreLayout = false;
        }

        if (handLayoutAnimator != null)
        {
            handLayoutAnimator.AnimateLayoutShift();
        }
        else if (dragOriginalParent is RectTransform parentRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
        }

        CaptureVisualBaseState();
    }

    private IEnumerator ReturnDraggedCardRoutine(RectTransform rectTransform)
    {
        Vector2 from = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        Vector3 fromScale = transform.localScale;
        float startTime = Time.realtimeSinceStartup;
        const float duration = 0.16f;

        while (true)
        {
            if (rectTransform == null)
            {
                yield break;
            }

            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(from, dragOriginalAnchoredPosition, eased);
            transform.localScale = Vector3.LerpUnclamped(fromScale, dragOriginalScale, eased);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        rectTransform.anchoredPosition = dragOriginalAnchoredPosition;
        transform.localScale = dragOriginalScale;
        if (dragLayoutElement != null)
        {
            dragLayoutElement.ignoreLayout = false;
        }
        CaptureVisualBaseState();
        returnDragRoutine = null;
    }

    private void PlayInsufficientLuxFeedback()
    {
        StartClickFeedback(InsufficientLuxFeedbackRoutine());
    }

    private void StartClickFeedback(IEnumerator routine)
    {
        if (clickFeedbackRoutine != null)
        {
            StopCoroutine(clickFeedbackRoutine);
            RestoreClickFeedbackState();
        }

        clickFeedbackRoutine = StartCoroutine(routine);
    }

    private IEnumerator ClickBoingRoutine()
    {
        Transform target = transform;
        clickFeedbackBaseScale = GetRestScale();
        RectTransform rectTransform = transform as RectTransform;
        clickFeedbackBasePosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        clickFeedbackBaseColor = visualBaseColor;

        yield return ScaleOver(transform.localScale, clickFeedbackBaseScale * 1.03f, 0.045f);
        yield return ScaleOver(clickFeedbackBaseScale * 1.03f, clickFeedbackBaseScale, 0.065f);

        target.localScale = GetRestScale();
        clickFeedbackRoutine = null;
    }

private IEnumerator InsufficientLuxFeedbackRoutine()
    {
        RectTransform rectTransform = transform as RectTransform;
        Image image = GetComponent<Image>();

        clickFeedbackBaseScale = GetRestScale();
        clickFeedbackBasePosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        clickFeedbackBaseColor = visualBaseColor;

        Color restColor = GetRestColor();
        Color warningColor = Color.Lerp(visualBaseColor, new Color(1f, 0.08f, 0.12f, visualBaseColor.a), 0.42f);
        warningColor.a = visualBaseColor.a;
        if (image != null)
        {
            image.color = warningColor;
        }

        float startTime = Time.realtimeSinceStartup;
        const float duration = 0.24f;
        const float distance = 18f;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float offset = Mathf.Sin(t * Mathf.PI * 8f) * distance * (1f - t);

            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = clickFeedbackBasePosition + new Vector2(offset, 0f);
            }

            if (image != null)
            {
                image.color = Color.Lerp(warningColor, restColor, t);
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        RestoreClickFeedbackState();
        clickFeedbackRoutine = null;
    }

    private IEnumerator ScaleOver(Vector3 from, Vector3 to, float duration)
    {
        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transform.localScale = Vector3.LerpUnclamped(from, to, eased);

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
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

    private void RestoreClickFeedbackState()
    {
        transform.localScale = GetRestScale();

        RectTransform rectTransform = transform as RectTransform;
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = clickFeedbackBasePosition;
        }

        Image image = GetComponent<Image>();
        if (image != null)
        {
            image.color = GetRestColor();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (dragging)
        {
            return;
        }

        hovering = true;
        StartHoverFeedback(true);

        if (battleManager != null && cardData != null)
        {
            battleManager.ShowCardTooltip(cardData, transform as RectTransform);
        }
    }

public void OnPointerExit(PointerEventData eventData)
    {
        if (dragging)
        {
            return;
        }

        hovering = false;
        StartHoverFeedback(false);

        if (battleManager != null)
        {
            battleManager.HideCardTooltip();
        }
    }

    private void StartHoverFeedback(bool enter)
    {
        if (hoverFeedbackRoutine != null)
        {
            StopCoroutine(hoverFeedbackRoutine);
        }

        hoverFeedbackRoutine = StartCoroutine(HoverFeedbackRoutine(enter));
    }

    private IEnumerator HoverFeedbackRoutine(bool enter)
    {
        EnsureImageReference();

        Image image = cardImage != null ? cardImage : GetComponent<Image>();
        if (enter)
        {
            hoverBaseScale = visualBaseScale;
            hoverBaseColor = visualBaseColor;
        }

        Vector3 fromScale = transform.localScale;
        Vector3 toScale = enter ? GetHoverScale() : visualBaseScale;
        Color fromColor = image != null ? image.color : Color.white;
        Color toColor = enter ? GetHoverColor(visualBaseColor) : visualBaseColor;

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = hoverTweenDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / hoverTweenDuration);
            float eased = t * t * (3f - 2f * t);

            transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            if (image != null)
            {
                image.color = Color.Lerp(fromColor, toColor, eased);
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        transform.localScale = toScale;
        if (image != null)
        {
            image.color = toColor;
        }

        hoverFeedbackRoutine = null;
    }

    private Color GetHoverColor(Color baseColor)
    {
        Color hoverColor = Color.Lerp(baseColor, Color.black, hoverDarkenAmount);
        hoverColor.a = baseColor.a;
        return hoverColor;
    }

    private Vector3 GetHoverScale()
    {
        return visualBaseScale * hoverScale;
    }

    private Vector3 GetRestScale()
    {
        return hovering ? GetHoverScale() : visualBaseScale;
    }

    private Color GetRestColor()
    {
        return hovering ? GetHoverColor(visualBaseColor) : visualBaseColor;
    }


private void UpdateDragPlaceholder(bool shouldShow, Vector2 screenPosition)
    {
        if (dragOriginalParent == null)
        {
            return;
        }

        if (!shouldShow)
        {
            RemoveDragPlaceholder(true);
            return;
        }

        int targetIndex = GetPlaceholderSiblingIndex(screenPosition);
        if (dragPlaceholder == null)
        {
            if (handLayoutAnimator != null)
            {
                handLayoutAnimator.CaptureLayout();
            }

            dragPlaceholder = new GameObject("CardReturnPlaceholder", typeof(RectTransform), typeof(LayoutElement), typeof(CanvasGroup));
            dragPlaceholder.transform.SetParent(dragOriginalParent, false);
            ConfigureDragPlaceholder(dragPlaceholder.GetComponent<LayoutElement>());
            CanvasGroup group = dragPlaceholder.GetComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            dragPlaceholder.transform.SetSiblingIndex(Mathf.Clamp(targetIndex, 0, dragOriginalParent.childCount - 1));

            if (handLayoutAnimator != null)
            {
                handLayoutAnimator.AnimateLayoutShift();
            }
            return;
        }

        int clampedIndex = Mathf.Clamp(targetIndex, 0, dragOriginalParent.childCount - 1);
        if (dragPlaceholder.transform.GetSiblingIndex() != clampedIndex)
        {
            if (handLayoutAnimator != null)
            {
                handLayoutAnimator.CaptureLayout();
            }

            dragPlaceholder.transform.SetSiblingIndex(clampedIndex);

            if (handLayoutAnimator != null)
            {
                handLayoutAnimator.AnimateLayoutShift();
            }
        }
    }

    private void RemoveDragPlaceholder(bool animate)
    {
        if (dragPlaceholder == null)
        {
            return;
        }

        if (animate && handLayoutAnimator != null)
        {
            handLayoutAnimator.CaptureLayout();
        }

        Destroy(dragPlaceholder);
        dragPlaceholder = null;

        if (animate && handLayoutAnimator != null)
        {
            handLayoutAnimator.AnimateLayoutShift();
        }
    }

    private void ConfigureDragPlaceholder(LayoutElement layoutElement)
    {
        if (layoutElement == null)
        {
            return;
        }

        float width = dragLayoutElement != null && dragLayoutElement.preferredWidth > 0f ? dragLayoutElement.preferredWidth : dragOriginalSize.x;
        float height = dragLayoutElement != null && dragLayoutElement.preferredHeight > 0f ? dragLayoutElement.preferredHeight : dragOriginalSize.y;
        layoutElement.preferredWidth = Mathf.Max(1f, width);
        layoutElement.minWidth = Mathf.Max(1f, width);
        layoutElement.preferredHeight = Mathf.Max(1f, height);
        layoutElement.minHeight = Mathf.Max(1f, height);
        layoutElement.flexibleWidth = 0f;
        layoutElement.flexibleHeight = 0f;
    }

    private int GetPlaceholderSiblingIndex(Vector2 screenPosition)
    {
        if (dragOriginalParent == null)
        {
            return dragOriginalSiblingIndex;
        }

        Canvas parentCanvas = dragOriginalParent.GetComponentInParent<Canvas>();
        Camera camera = parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay ? parentCanvas.worldCamera : null;
        int fallbackIndex = Mathf.Clamp(dragOriginalSiblingIndex, 0, dragOriginalParent.childCount);
        int targetIndex = fallbackIndex;

        for (int i = 0; i < dragOriginalParent.childCount; i++)
        {
            Transform child = dragOriginalParent.GetChild(i);
            if (child == null || child.gameObject == dragPlaceholder)
            {
                continue;
            }

            RectTransform childRect = child as RectTransform;
            if (childRect == null)
            {
                continue;
            }

            Vector2 childScreen = RectTransformUtility.WorldToScreenPoint(camera, childRect.position);
            if (screenPosition.x > childScreen.x)
            {
                targetIndex = child.GetSiblingIndex() + 1;
            }
        }

        return Mathf.Clamp(targetIndex, 0, dragOriginalParent.childCount);
    }
}
