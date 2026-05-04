// InteractableBase.cs
using UnityEngine;

public abstract class InteractableBase : MonoBehaviour
{
    // 자식 클래스에서 구현
    public abstract void Interact(PlayerTopDown player);

    // 범위 내 진입 시 프롬프트 표시
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            SlumUIManager.Instance?.ShowPrompt("[Z] 상호작용");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            SlumUIManager.Instance?.HidePrompt();
    }
}