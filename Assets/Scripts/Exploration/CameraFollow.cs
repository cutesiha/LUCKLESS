// CameraFollow.cs
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 6f;

    // 맵 경계 (맵 크기에 맞게 Inspector에서 조정)
    public Vector2 mapMin = new Vector2(-12f, -12f);
    public Vector2 mapMax = new Vector2(12f, 12f);

    Camera cam;

    void Awake() { cam = GetComponent<Camera>(); }

    void LateUpdate()
    {
        if (target == null) return;

        float h = cam.orthographicSize;
        float w = h * cam.aspect;

        float x = Mathf.Clamp(target.position.x, mapMin.x + w, mapMax.x - w);
        float y = Mathf.Clamp(target.position.y, mapMin.y + h, mapMax.y - h);

        Vector3 dest = new Vector3(x, y, -10f);
        transform.position = Vector3.Lerp(transform.position, dest, smoothSpeed * Time.deltaTime);
    }
}