using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class FloatingMenuCard : MonoBehaviour
{
    [SerializeField] private Vector2 floatDistance = new Vector2(28f, 18f);
    [SerializeField] private float floatSpeed = 1f;
    [SerializeField] private float phaseOffset;
    [SerializeField] private float rotationDistance = 3f;
    [SerializeField] private float rotationSpeed = 0.75f;
    [SerializeField] private float scalePulse = 0.015f;

    private RectTransform rectTransform;
    private Vector2 origin;
    private float baseRotation;
    private Vector3 baseScale;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        origin = rectTransform.anchoredPosition;
        baseRotation = rectTransform.localEulerAngles.z;
        baseScale = rectTransform.localScale;
    }

    private void OnEnable()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        origin = rectTransform.anchoredPosition;
        baseRotation = rectTransform.localEulerAngles.z;
        baseScale = rectTransform.localScale;
    }

    private void Update()
    {
        float time = Time.unscaledTime;
        float horizontal = Mathf.Sin(time * floatSpeed + phaseOffset) * floatDistance.x;
        float vertical = Mathf.Cos(time * (floatSpeed * 0.73f) + phaseOffset * 1.37f) * floatDistance.y;
        float angle = Mathf.Sin(time * rotationSpeed + phaseOffset * 0.61f) * rotationDistance;
        float scale = 1f + Mathf.Sin(time * (floatSpeed * 0.49f) + phaseOffset) * scalePulse;

        rectTransform.anchoredPosition = origin + new Vector2(horizontal, vertical);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, baseRotation + angle);
        rectTransform.localScale = baseScale * scale;
    }

    public void Configure(Vector2 distance, float speed, float phase, float rotationAmount, float rotationWaveSpeed, float pulseAmount)
    {
        floatDistance = distance;
        floatSpeed = speed;
        phaseOffset = phase;
        rotationDistance = rotationAmount;
        rotationSpeed = rotationWaveSpeed;
        scalePulse = pulseAmount;
    }
}
