using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BattleHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] [Range(0f, 1f)] private float hoverDarkenAmount = 0.24f;
    [SerializeField] private float hoverScale = 1.025f;
    [SerializeField] private float hoverTweenDuration = 0.12f;

    private Graphic targetGraphic;
    private Coroutine routine;
    private Vector3 baseScale = Vector3.one;
    private Color baseColor = Color.white;

    private void Awake()
    {
        CacheTargets();
    }

    private void OnEnable()
    {
        CacheTargets();
        baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
        if (targetGraphic != null)
        {
            baseColor = targetGraphic.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StartHover(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartHover(false);
    }

    private void CacheTargets()
    {
        if (targetGraphic != null)
        {
            return;
        }

        targetGraphic = GetComponent<Graphic>();
        if (targetGraphic == null)
        {
            targetGraphic = GetComponentInChildren<Graphic>(true);
        }
    }

    private void StartHover(bool enter)
    {
        CacheTargets();

        if (enter)
        {
            baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            if (targetGraphic != null)
            {
                baseColor = targetGraphic.color;
            }
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(HoverRoutine(enter));
    }

    private IEnumerator HoverRoutine(bool enter)
    {
        Vector3 fromScale = transform.localScale;
        Vector3 toScale = enter ? baseScale * hoverScale : baseScale;
        Color fromColor = targetGraphic != null ? targetGraphic.color : Color.white;
        Color toColor = enter ? GetHoverColor(baseColor) : baseColor;

        float startTime = Time.realtimeSinceStartup;
        while (true)
        {
            float elapsed = Time.realtimeSinceStartup - startTime;
            float t = hoverTweenDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / hoverTweenDuration);
            float eased = t * t * (3f - 2f * t);

            transform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
            if (targetGraphic != null)
            {
                targetGraphic.color = Color.Lerp(fromColor, toColor, eased);
            }

            if (t >= 1f)
            {
                break;
            }

            yield return null;
        }

        transform.localScale = toScale;
        if (targetGraphic != null)
        {
            targetGraphic.color = toColor;
        }

        routine = null;
    }

    private Color GetHoverColor(Color color)
    {
        Color hoverColor = Color.Lerp(color, Color.black, hoverDarkenAmount);
        hoverColor.a = color.a;
        return hoverColor;
    }
}
