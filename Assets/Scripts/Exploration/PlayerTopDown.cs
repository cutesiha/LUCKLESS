using UnityEngine;

public class PlayerTopDown : MonoBehaviour
{
    public float moveSpeed = 2f;
    public bool isLocked = false;

    private Rigidbody2D rb;
    private Vector2 inputDir;
    private Vector2 mouseTarget;
    private bool moveToMouse = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (isLocked)
        {
            inputDir = Vector2.zero;
            return;
        }

        // 마우스 클릭하면 그 위치로 이동
        if (Input.GetMouseButtonDown(0))
        {
            mouseTarget = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            moveToMouse = true;
        }

        // 키보드 누르면 마우스 이동 취소
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (h != 0 || v != 0)
        {
            moveToMouse = false;
            inputDir = new Vector2(h, v).normalized;
        }
        else if (moveToMouse)
        {
            Vector2 dir = mouseTarget - rb.position;

            // 목표 지점에 거의 도달하면 멈춤
            if (dir.magnitude < 0.1f)
            {
                moveToMouse = false;
                inputDir = Vector2.zero;
            }
            else
            {
                inputDir = dir.normalized;
            }
        }
        else
        {
            inputDir = Vector2.zero;
        }
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + inputDir * moveSpeed * Time.fixedDeltaTime);
    }
}