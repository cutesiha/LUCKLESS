using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothSpeed = 6f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 dest = new Vector3(target.position.x, target.position.y, -10f);
        transform.position = Vector3.Lerp(transform.position, dest, smoothSpeed * Time.deltaTime);
    }
}