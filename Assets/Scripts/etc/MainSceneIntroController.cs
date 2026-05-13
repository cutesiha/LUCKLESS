using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainSceneIntroController : MonoBehaviour
{
    [Header("Title")]
    [SerializeField] private CanvasGroup titleGroup;
    [SerializeField] private RectTransform titleImage;
    [SerializeField] private RectTransform topEyeCover;
    [SerializeField] private RectTransform bottomEyeCover;
    [SerializeField] private float titleFadeDuration = 1f;
    [SerializeField] private float titleHoldSeconds = 2f;
    [SerializeField] private float titleFadeAfterEyeDelay = 1f;
    [SerializeField] private float eyeOpenDuration = 1.15f;

    [Header("Panels")]
    [SerializeField] private GameObject missionPanel;
    [SerializeField] private GameObject handbookPanelPrefab;
    [SerializeField] private Transform handbookParent;
    [SerializeField] private GameObject inventoryPanel;

    [Header("Audio")]
    [SerializeField] private AudioSource[] musicSources;
    [SerializeField] private float musicFadeInDuration = 1.5f;

    private Vector2 topEyeClosedPosition;
    private Vector2 bottomEyeClosedPosition;
    private GameObject handbookPanelInstance;
    private float[] musicTargetVolumes;

    private void Awake()
    {
        PrepareMusicFadeIn();
        CacheEyeCoverPositions();
        SetMissionVisible(false);
        SetHandbookVisible(false);
        SetEyeCoversVisible(false);
        SetTitleVisible(false, 0f);
    }

    private IEnumerator Start()
    {
        StartCoroutine(FadeInMusic());
        SetTitleVisible(true, 0f);
        yield return FadeGroup(titleGroup, 0f, 1f, titleFadeDuration, true);
        yield return new WaitForSecondsRealtime(titleHoldSeconds);
        yield return OpenEyeCovers();
        yield return new WaitForSecondsRealtime(titleFadeAfterEyeDelay);
        SetTitleVisible(false, 0f);
        SetMissionVisible(true);
        SetHandbookVisible(true);
    }

    private void SetMissionVisible(bool visible)
    {
        if (missionPanel != null)
        {
            missionPanel.SetActive(visible);
        }
    }

    private void SetHandbookVisible(bool visible)
    {
        if (!visible)
        {
            if (handbookPanelInstance != null)
            {
                handbookPanelInstance.SetActive(false);
            }
            return;
        }

        if (handbookPanelInstance == null && handbookPanelPrefab != null)
        {
            Transform parent = handbookParent != null ? handbookParent : transform;
            handbookPanelInstance = Instantiate(handbookPanelPrefab, parent);
            handbookPanelInstance.name = "HandbookPanel";
            NormalizeHandbookPanel(handbookPanelInstance);
        }

        if (handbookPanelInstance != null)
        {
            handbookPanelInstance.SetActive(true);
            handbookPanelInstance.transform.SetAsLastSibling();
            HandbookPanelController controller = handbookPanelInstance.GetComponent<HandbookPanelController>();
            if (controller != null)
            {
                controller.SetInventoryPanel(inventoryPanel);
            }
        }
    }

    private void NormalizeHandbookPanel(GameObject panel)
    {
        RectTransform rectTransform = panel.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.localScale = Vector3.one;
    }

    private void SetTitleVisible(bool visible, float alpha)
    {
        if (titleImage != null)
        {
            titleImage.gameObject.SetActive(visible);
            titleImage.SetAsLastSibling();
        }

        if (titleGroup != null)
        {
            titleGroup.alpha = alpha;
            titleGroup.interactable = visible;
            titleGroup.blocksRaycasts = visible;
            titleGroup.gameObject.SetActive(visible || alpha > 0f);
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
        group.interactable = activeAfter;
        group.blocksRaycasts = activeAfter;
        group.gameObject.SetActive(activeAfter || to > 0f);
    }

    private void PrepareMusicFadeIn()
    {
        if (musicSources == null || musicSources.Length == 0)
        {
#if UNITY_2023_1_OR_NEWER
            musicSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
            musicSources = FindObjectsOfType<AudioSource>(true);
#endif
        }

        List<AudioSource> playableSources = new List<AudioSource>();
        if (musicSources != null)
        {
            for (int i = 0; i < musicSources.Length; i++)
            {
                if (musicSources[i] != null && musicSources[i].clip != null)
                {
                    playableSources.Add(musicSources[i]);
                }
            }
        }

        musicSources = playableSources.ToArray();
        musicTargetVolumes = new float[musicSources.Length];

        for (int i = 0; i < musicTargetVolumes.Length; i++)
        {
            AudioSource source = musicSources[i];
            GameAudioSettings.ApplyBgmSource(source, source.volume);
            musicTargetVolumes[i] = source.volume;
            source.volume = 0f;

            if (!source.isPlaying)
            {
                source.Play();
            }
        }
    }

    private IEnumerator FadeInMusic()
    {
        if (musicSources == null || musicSources.Length == 0 || musicTargetVolumes == null)
        {
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = musicFadeInDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / musicFadeInDuration);

            for (int i = 0; i < musicSources.Length && i < musicTargetVolumes.Length; i++)
            {
                if (musicSources[i] != null)
                {
                    musicSources[i].volume = Mathf.Lerp(0f, musicTargetVolumes[i], t);
                }
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }
}
