using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SmoothHandLayoutAnimator : MonoBehaviour
{
    [SerializeField] private float animationDuration = 0.22f;

    private readonly Dictionary<RectTransform, Vector2> beforePositions = new Dictionary<RectTransform, Vector2>();
    private RectTransform rectTransform;
    private LayoutGroup[] layoutGroups;
    private ContentSizeFitter[] fitters;
    private Coroutine animationRoutine;

    private void Awake()
    {
        CacheComponents();
    }

    private void CacheComponents()
    {
        if (rectTransform == null)
        {
            rectTransform = transform as RectTransform;
        }

        layoutGroups = GetComponents<LayoutGroup>();
        fitters = GetComponents<ContentSizeFitter>();
    }

    public void CaptureLayout()
    {
        CompleteAnimation();
        CacheComponents();
        beforePositions.Clear();

        if (rectTransform == null)
        {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            beforePositions[child] = child.anchoredPosition;
        }
    }

    public void AnimateLayoutShift()
    {
        if (!isActiveAndEnabled || rectTransform == null)
        {
            beforePositions.Clear();
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(AnimateLayoutShiftRoutine());
    }

    private IEnumerator AnimateLayoutShiftRoutine()
    {
        CacheComponents();
        SetLayoutEnabled(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);

        Dictionary<RectTransform, Vector2> targets = new Dictionary<RectTransform, Vector2>();
        for (int i = 0; i < transform.childCount; i++)
        {
            RectTransform child = transform.GetChild(i) as RectTransform;
            if (child == null || !child.gameObject.activeSelf)
            {
                continue;
            }

            targets[child] = child.anchoredPosition;
        }

        SetLayoutEnabled(false);

        foreach (KeyValuePair<RectTransform, Vector2> target in targets)
        {
            if (target.Key == null)
            {
                continue;
            }

            if (beforePositions.TryGetValue(target.Key, out Vector2 before))
            {
                target.Key.anchoredPosition = before;
            }
        }

        float startTime = Time.realtimeSinceStartup;
        float duration = Mathf.Max(0.01f, animationDuration);
        while (true)
        {
            float t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            foreach (KeyValuePair<RectTransform, Vector2> target in targets)
            {
                RectTransform child = target.Key;
                if (child == null)
                {
                    continue;
                }

                Vector2 from = beforePositions.TryGetValue(child, out Vector2 before) ? before : target.Value;
                child.anchoredPosition = Vector2.LerpUnclamped(from, target.Value, eased);
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        foreach (KeyValuePair<RectTransform, Vector2> target in targets)
        {
            if (target.Key != null)
            {
                target.Key.anchoredPosition = target.Value;
            }
        }

        SetLayoutEnabled(true);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        beforePositions.Clear();
        animationRoutine = null;
    }

    private void CompleteAnimation()
    {
        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
            animationRoutine = null;
        }

        SetLayoutEnabled(true);
    }

    private void SetLayoutEnabled(bool enabled)
    {
        if (layoutGroups == null || fitters == null)
        {
            CacheComponents();
        }

        if (layoutGroups != null)
        {
            for (int i = 0; i < layoutGroups.Length; i++)
            {
                if (layoutGroups[i] != null)
                {
                    layoutGroups[i].enabled = enabled;
                }
            }
        }

        if (fitters != null)
        {
            for (int i = 0; i < fitters.Length; i++)
            {
                if (fitters[i] != null)
                {
                    fitters[i].enabled = enabled;
                }
            }
        }
    }
}
