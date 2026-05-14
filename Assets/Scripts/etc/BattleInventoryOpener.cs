using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BattleInventoryOpener : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;

    private Button button;

    private void Awake()
    {
        EnsureButton();
    }

    private void OnEnable()
    {
        EnsureButton();
    }

    private void EnsureButton()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            button = gameObject.AddComponent<Button>();
        }

        Image image = GetComponent<Image>();
        image.raycastTarget = true;

        button.transition = Selectable.Transition.None;
        button.targetGraphic = image;
        button.onClick.RemoveListener(OpenInventory);
        button.onClick.AddListener(OpenInventory);
    }

    public void OpenInventory()
    {
        if (inventoryPanel == null)
        {
            return;
        }

        inventoryPanel.transform.localScale = Vector3.one;
        inventoryPanel.SetActive(true);
        inventoryPanel.transform.SetAsLastSibling();
    }
}
