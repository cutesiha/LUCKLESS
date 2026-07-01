using System;
using System.Collections;
using UnityEngine;

public class UIPanelBoingAnimator : MonoBehaviour
{
    [SerializeField] private float openDuration = 0.17f;
    [SerializeField] private float closeDuration = 0.15f;
    [SerializeField] private float openStartScale = 0.92f;
    [SerializeField] private float openOvershootScale = 1.03f;
    [SerializeField] private float closeOvershootScale = 1.03f;
    [SerializeField] private float closeEndScale = 0.9f;

    private Coroutine routine;
    private Vector3 baseScale = Vector3.one;
    private bool closing;

    private void Awake()
    {
        baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
    }

    private void OnEnable()
    {
        if (!closing)
        {
            PlayOpen();
        }
    }

    public void PlayOpen()
    {
        closing = false;
        StartAnimation(OpenRoutine());
    }

    public void PlayClose(Action onComplete)
    {
        closing = true;
        StartAnimation(CloseRoutine(onComplete));
    }

    private void StartAnimation(IEnumerator animation)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(animation);
    }

    private IEnumerator OpenRoutine()
    {
        yield return ScaleOver(baseScale * openStartScale, baseScale * openOvershootScale, openDuration * 0.62f, EaseOutBack);
        yield return ScaleOver(baseScale * openOvershootScale, baseScale, openDuration * 0.38f, SmoothStep);
        transform.localScale = baseScale;
        routine = null;
    }

    private IEnumerator CloseRoutine(Action onComplete)
    {
        yield return ScaleOver(transform.localScale, baseScale * closeOvershootScale, closeDuration * 0.42f, SmoothStep);
        yield return ScaleOver(baseScale * closeOvershootScale, baseScale * closeEndScale, closeDuration * 0.58f, SmoothStep);
        transform.localScale = baseScale;
        closing = false;
        routine = null;
        onComplete?.Invoke();
    }

    private IEnumerator ScaleOver(Vector3 from, Vector3 to, float duration, Func<float, float> ease)
    {
        if (duration <= 0f)
        {
            transform.localScale = to;
            yield break;
        }

        float startTime = Time.realtimeSinceStartup;

        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.LerpUnclamped(from, to, ease(t));

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }
    }

    private static float SmoothStep(float t)
    {
        return t * t * (3f - 2f * t);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
