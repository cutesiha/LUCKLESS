// PlayerTopDown.cs
using UnityEngine;

public class PlayerTopDown : MonoBehaviour
{
    public float moveSpeed = 3.5f;
    public Animator animator;

    private Rigidbody2D rb;
    private Vector2 inputDir;
    public bool isLocked = false;  // 대화/이벤트 중 이동 잠금

    // 마지막으로 바라본 방향 (상호작용용)
    public Vector2 lastDir = Vector2.down;

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

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // RPG Maker 느낌: 대각선 이동 없음
        if (h != 0) v = 0;

        inputDir = new Vector2(h, v);

        if (inputDir != Vector2.zero)
            lastDir = inputDir;

        // 애니메이터에 방향 전달
        if (animator != null)
        {
            animator.SetFloat("MoveX", inputDir.x);
            animator.SetFloat("MoveY", inputDir.y);
            animator.SetBool("IsMoving", inputDir != Vector2.zero);
        }

        // Z키 or Enter 상호작용
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Return))
            TryInteract();
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + inputDir * moveSpeed * Time.fixedDeltaTime);
    }

    void TryInteract()
    {
        // 플레이어 앞 방향으로 짧게 레이캐스트
        RaycastHit2D hit = Physics2D.Raycast(
            rb.position + lastDir * 0.3f,
            lastDir,
            0.8f,
            LayerMask.GetMask("Interactable")
        );

        if (hit.collider != null)
        {
            var obj = hit.collider.GetComponent<InteractableBase>();
            if (obj != null) obj.Interact(this);
        }
    }
}