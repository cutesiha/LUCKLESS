using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class StartMenuButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public enum ClickAction
    {
        None,
        LoadPrologue,
        QuitGame,
        AutoByObjectName
    }

    [Header("Hover")]
    [SerializeField] private float hoverOffsetX = 34f;
    [SerializeField] private float hoverMoveTime = 0.16f;

    [Header("Click")]
    [SerializeField] private ClickAction clickAction = ClickAction.AutoByObjectName;
    [SerializeField] private string prologueSceneName = "Prologue";
    [SerializeField] private float flashTime = 0.18f;
    [SerializeField] private float fadeTime = 0.8f;
    [SerializeField] private float audioFadeTime = 2f;
    [SerializeField] private float sceneLoadDelayAfterFade = 2f;
    [SerializeField] private AudioSource[] audioSourcesToFade;

    [Header("SFX")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip clickClip;

    private RectTransform rectTransform;
    private Image image;
    private Vector2 restPosition;
    private Coroutine moveRoutine;
    private bool pointerInside;
    private bool isTransitioning;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        image.raycastTarget = true;
    }

    private void OnEnable()
    {
        pointerInside = false;
        isTransitioning = false;
        restPosition = rectTransform.anchoredPosition;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isTransitioning)
        {
            return;
        }

        pointerInside = true;
        restPosition = rectTransform.anchoredPosition;
        MoveTo(restPosition + Vector2.right * hoverOffsetX);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isTransitioning)
        {
            return;
        }

        pointerInside = false;
        MoveTo(restPosition);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isTransitioning)
        {
            return;
        }

        ClickAction resolvedAction = ResolveAction();

        if (resolvedAction == ClickAction.None)
        {
            return;
        }

        PlayClickSfx();
        StartCoroutine(PlayClickAndRun(resolvedAction));
    }

    private void PlayClickSfx()
    {
        if (sfxSource == null || clickClip == null)
        {
            return;
        }

        float volume = Mathf.Clamp01(PlayerPrefs.GetFloat("SFX", 1f));
        sfxSource.PlayOneShot(clickClip, volume);
    }

    private ClickAction ResolveAction()
    {
        if (clickAction != ClickAction.AutoByObjectName)
        {
            return clickAction;
        }

        if (name.EndsWith("_01"))
        {
            return ClickAction.LoadPrologue;
        }

        if (name.EndsWith("_04"))
        {
            return ClickAction.QuitGame;
        }

        return ClickAction.None;
    }

    private void MoveTo(Vector2 targetPosition)
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
        }

        moveRoutine = StartCoroutine(MoveRoutine(targetPosition));
    }

    private IEnumerator MoveRoutine(Vector2 targetPosition)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while (elapsed < hoverMoveTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / hoverMoveTime);
            t = t * t * (3f - 2f * t);
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        moveRoutine = null;
    }

    private IEnumerator PlayClickAndRun(ClickAction resolvedAction)
    {
        isTransitioning = true;

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (!pointerInside)
        {
            rectTransform.anchoredPosition = restPosition;
        }

        yield return FlashButton();
        yield return FadeOutScreenAndAudio();

        if (resolvedAction == ClickAction.LoadPrologue)
        {
            yield return new WaitForSecondsRealtime(sceneLoadDelayAfterFade);
            SceneManager.LoadScene(prologueSceneName);
            yield break;
        }

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private IEnumerator FlashButton()
    {
        Image flashImage = CreateFlashImage();
        Color originalColor = image.color;
        Vector3 originalScale = rectTransform.localScale;
        float elapsed = 0f;

        while (elapsed < flashTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flashTime);
            float pulse = Mathf.Sin(t * Mathf.PI);
            image.color = Color.Lerp(originalColor, Color.white, pulse);
            flashImage.color = new Color(1f, 1f, 1f, pulse * 0.65f);
            rectTransform.localScale = originalScale * (1f + pulse * 0.035f);
            yield return null;
        }

        image.color = originalColor;
        rectTransform.localScale = originalScale;
        Destroy(flashImage.gameObject);
    }

    private Image CreateFlashImage()
    {
        GameObject flashObject = new GameObject("ButtonFlash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        flashObject.transform.SetParent(transform, false);
        flashObject.transform.SetAsLastSibling();

        RectTransform flashRect = flashObject.GetComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        flashRect.localScale = Vector3.one;

        Image flashImage = flashObject.GetComponent<Image>();
        flashImage.sprite = image.sprite;
        flashImage.type = image.type;
        flashImage.preserveAspect = image.preserveAspect;
        flashImage.raycastTarget = false;
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        return flashImage;
    }

    private IEnumerator FadeOutScreenAndAudio()
    {
        Coroutine audioFadeRoutine = StartCoroutine(FadeOutAudioSources());
        yield return FadeScreenToBlack();

        if (audioFadeRoutine != null)
        {
            yield return audioFadeRoutine;
        }
    }

    private IEnumerator FadeOutAudioSources()
    {
        AudioSource[] sources = GetAudioSourcesToFade();

        if (sources.Length == 0)
        {
            yield break;
        }

        float[] startVolumes = new float[sources.Length];

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
            {
                startVolumes[i] = sources[i].volume;
            }
        }

        float elapsed = 0f;

        while (elapsed < audioFadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / audioFadeTime);

            for (int i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null)
                {
                    sources[i].volume = Mathf.Lerp(startVolumes[i], 0f, t);
                }
            }

            yield return null;
        }

        for (int i = 0; i < sources.Length; i++)
        {
            if (sources[i] != null)
            {
                sources[i].volume = 0f;
                sources[i].Stop();
            }
        }
    }

    private AudioSource[] GetAudioSourcesToFade()
    {
        if (audioSourcesToFade != null && audioSourcesToFade.Length > 0)
        {
            return audioSourcesToFade;
        }

        return FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
    }

    private IEnumerator FadeScreenToBlack()
    {
        Image fadeImage = CreateFadeImage();
        Color color = Color.black;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeTime);
            fadeImage.color = color;
            yield return null;
        }

        color.a = 1f;
        fadeImage.color = color;
    }

    private Image CreateFadeImage()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        GameObject fadeObject = new GameObject("StartMenuFade", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        fadeObject.transform.SetParent(canvas != null ? canvas.transform : transform.root, false);
        fadeObject.transform.SetAsLastSibling();

        RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.offsetMin = Vector2.zero;
        fadeRect.offsetMax = Vector2.zero;

        Image fadeImage = fadeObject.GetComponent<Image>();
        fadeImage.raycastTarget = true;
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        return fadeImage;
    }
}
